using System.Diagnostics;
using System.Text;
using FreeRedis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 处理等待超时
    /// </summary>
    internal class HandlePendingTimeOutService
    (
        ConsumerServiceSelector consumerServiceSelector,
        QueueConfig queueConfig,
        CacheKeyManager cacheKeyManager,
        IRedisClient redisClient,
        ILogger<HandlePendingTimeOutService> logger
    )
        : BackgroundService
    {
        private readonly string _serverName = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerExecutorDescriptors = consumerServiceSelector.GetConsumerExecutorDescriptors().ToArray();
            RedisStreamHelper.EnsureConsumerGroups(redisClient, cacheKeyManager, consumerExecutorDescriptors);

            var tasks = new List<Task>();
            foreach (var consumerExecutorDescriptor in consumerExecutorDescriptors.AsParallel())
            {
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;
                var automaticRetryAttempts = consumerExecutorDescriptor.AutomaticRetryAttempts;
                var pendingTimeOut = consumerExecutorDescriptor.PendingTimeOut * 1000L;
                var queueNameKey = cacheKeyManager.GetQueueName(queueName);

                tasks.Add(Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var readPendingLock = redisClient.Lock($"{cacheKeyManager.ReadPendingLock}:{queueName}", 30);
                            if (readPendingLock == null)
                            {
                                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                                continue;
                            }
                            try
                            {
                                var hasClaimedMessage = false;
                                var timeoutConsumerName = $"{_serverName}:Timeout:{queueName}";
                                var pendingStartId = "-";
                                do
                                {
                                    var pendingResults = await redisClient.XPendingAsync(queueNameKey, groupName, pendingStartId, "+", 100, null!).ConfigureAwait(false);
                                    if (pendingResults is not { Length: > 0 })
                                        break;

                                    pendingStartId = GetNextStreamId(pendingResults[^1].id);
                                    var timeoutMessageIds = pendingResults
                                        .Where(x => !string.IsNullOrEmpty(x.id) && x.id != "0-0" && x.id != "-" && x.id != "+" && x.id != "$" && x.id != ">")
                                        .Where(x => x.idle >= pendingTimeOut)
                                        .Select(x => x.id)
                                        .ToArray();
                                    if (timeoutMessageIds.Length == 0)
                                        continue;

                                    var entries = await redisClient.XClaimAsync(queueNameKey, groupName, timeoutConsumerName, pendingTimeOut, timeoutMessageIds).ConfigureAwait(false);
                                    if (entries is { Length: > 0 })
                                    {
                                        hasClaimedMessage = true;
                                        foreach (var entry in entries)
                                        {
                                            var message = entry.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);
                                            if (message != null)
                                            {
                                                message.CreateTimestamp = TimeHelper.GetMillisecondTimestamp();

                                                //分为2种情况, 普通的未正常ACK超时, 一种是错误超时
                                                //错误超时
                                                var messageErrorInfo = default(MessageErrorInfo);
                                                var errorInfoStr = redisClient.HGet<string>($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "ErrorInfo");
                                                if (errorInfoStr != null)
                                                    messageErrorInfo = queueConfig.Serializer.Deserialize<MessageErrorInfo>(errorInfoStr);

                                                if (messageErrorInfo != null)
                                                {
                                                    var executionTimes = redisClient.HGet<int>($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "ExecutionTimes");
                                                    //超出了重试次数, 则删除队列消息, 并添加到错误列表
                                                    if (executionTimes > automaticRetryAttempts)
                                                    {
                                                        using var pipeError = redisClient.StartPipe();
                                                        pipeError.XAck(queueNameKey, groupName, entry.id);
                                                        pipeError.XDel(queueNameKey, entry.id);
                                                        pipeError.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Failed);
                                                        pipeError.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                                        pipeError.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);
                                                        pipeError.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                        pipeError.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                                        pipeError.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                        pipeError.EndPipe();
                                                        continue;
                                                    }
                                                }

                                                var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(message));

                                                var newStreamMessageId = await redisClient.XAddAsync(queueNameKey, data).ConfigureAwait(false);

                                                using var pipe = redisClient.StartPipe();
                                                pipe.XAck(queueNameKey, groupName, entry.id);
                                                pipe.XDel(queueNameKey, entry.id);
                                                pipe.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Id", newStreamMessageId, "Status", MessageStatus.Pending);
                                                pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);
                                                pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), message.MessageId);
                                                pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                                pipe.EndPipe();

                                                logger?.LogInformation("Queue {queueName} republish pending timeout message {content}", queueName, message.MessageContent);
                                            }
                                            else
                                            {
                                                long.TryParse(entry.id.Split("-").FirstOrDefault(), out var messageTime);
                                                var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - messageTime > pendingTimeOut;
                                                if (isTimeOutMessage)
                                                {
                                                    using var pipe = redisClient.StartPipe();
                                                    pipe.XAck(queueNameKey, groupName, entry.id);
                                                    pipe.XDel(queueNameKey, entry.id);
                                                    pipe.EndPipe();
                                                }
                                            }
                                        }
                                    }
                                } while (!stoppingToken.IsCancellationRequested && pendingStartId != "0-0");

                                if (!hasClaimedMessage)
                                {
                                    await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError(ex, "Handle timeout data exception!");
                            }
                            finally
                            {
                                readPendingLock.Unlock();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Handle timeout lock exception!");
                        }
                    }
                }, stoppingToken));
            }

            return Task.WhenAll(tasks);
        }

        private static string GetNextStreamId(string? streamId)
        {
            if (string.IsNullOrEmpty(streamId))
                return "0-0";

            var parts = streamId.Split('-');
            if (parts.Length != 2 || !long.TryParse(parts[1], out var sequence))
                return streamId;

            return $"{parts[0]}-{sequence + 1}";
        }
    }
}
