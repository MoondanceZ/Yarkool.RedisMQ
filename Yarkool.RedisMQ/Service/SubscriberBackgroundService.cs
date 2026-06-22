using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ;

public class ConsumerBackgroundService
(
    ConsumerServiceSelector consumerServiceSelector,
    QueueConfig queueConfig,
    CacheKeyManager cacheKeyManager,
    IRedisClient redisClient,
    IServiceProvider serviceProvider,
    ILogger<ConsumerBackgroundService> logger
)
    : BackgroundService
{
    private readonly string _serverName = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerExecutorDescriptors = consumerServiceSelector.GetConsumerExecutorDescriptors().ToArray();
        RedisStreamHelper.EnsureConsumerGroups(redisClient, cacheKeyManager, consumerExecutorDescriptors);

        var tasks = new List<Task>
        {
            RunLiveServerAsync(stoppingToken)
        };

        foreach (var consumerExecutorDescriptor in consumerExecutorDescriptors.AsParallel())
        {
            var consumerType = consumerExecutorDescriptor.ConsumerTypeInfo;
            var messageType = consumerExecutorDescriptor.MessageTypeInfo;
            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;
            var consumerName = consumerExecutorDescriptor.ConsumerName;
            var automaticRetryAttempts = consumerExecutorDescriptor.AutomaticRetryAttempts;
            var isDelayQueueConsumer = consumerExecutorDescriptor.IsDelayQueueConsumer;
            var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
            var isAutoAck = consumerExecutorDescriptor.IsAutoAck;
            var queueNameKey = cacheKeyManager.GetQueueName(queueName);

            if (isDelayQueueConsumer)
                tasks.Add(Task.Run(() => ExecuteDelayQueuePollingAsync(consumerExecutorDescriptor, stoppingToken), stoppingToken));

            for (var i = 0; i < consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount; i++)
            {
                var consumerIndex = i + 1;
                var curConsumerName = $"{_serverName}:{consumerName}:{consumerIndex}";
                redisClient.SAdd(cacheKeyManager.ConsumerList, curConsumerName);
                tasks.Add(Task.Run(async () =>
                {
                    logger.LogInformation($"{curConsumerName} subscribing");
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var message = default(BaseMessage);
                        var messageContent = default(object);
                        // >：读取最新的消息（尚未分配给某个 consumer 的消息）
                        try
                        {
                            var streamsEntryResults = await redisClient.XReadGroupAsync(groupName, curConsumerName, prefetchCount, 5 * 1000, false, queueNameKey, ">").ConfigureAwait(false);
                            if (streamsEntryResults != null && streamsEntryResults.Length != 0)
                            {
                                var entryResultEntries = streamsEntryResults.First()?.entries;
                                if (entryResultEntries?.Length > 0)
                                {
                                    foreach (var data in entryResultEntries)
                                    {
                                        var messageHandler = new ConsumerMessageHandler(queueNameKey, groupName, redisClient, cacheKeyManager);
                                        var time = DateTime.Now.ToString("yyyyMMddHH");
                                        try
                                        {
                                            using var scope = serviceProvider.CreateScope();
                                            var consumer = (IRedisMQConsumerExecutor)scope.ServiceProvider.GetRequiredService(consumerType);
                                            message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);
                                            messageHandler.MessageId = message.MessageId;
                                            messageHandler.StreamMessageId = data.id;

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} subscribing {message.MessageContent}");
                                            messageContent = string.IsNullOrEmpty(message.MessageContent) ? null : queueConfig.Serializer.Deserialize(message.MessageContent, messageType);

                                            using var pipe = redisClient.StartPipe();
                                            pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                            pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                            pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), message.MessageId);
                                            pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                            pipe.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Processing);
                                            pipe.HIncrBy($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "ExecutionTimes", 1);
                                            pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                            pipe.EndPipe();

                                            //Execute message
                                            await consumer.ExecuteAsync(messageContent, messageHandler, stoppingToken).ConfigureAwait(false);

                                            if (isAutoAck)
                                            {
                                                using var tran = redisClient.Multi();
                                                tran.IncrBy($"{cacheKeyManager.ConsumeSucceeded}:Total", 1);
                                                tran.IncrBy($"{cacheKeyManager.ConsumeSucceeded}:{time}", 1);
                                                tran.Expire($"{cacheKeyManager.ConsumeSucceeded}:{time}", TimeSpan.FromHours(30));
                                                tran.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Completed);
                                                tran.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);
                                                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), message.MessageId);
                                                if (isAutoAck)
                                                {
                                                    //ACK
                                                    tran.XAck(queueNameKey, groupName, data.id);
                                                    tran.XDel(queueNameKey, data.id);
                                                    tran.IncrBy($"{cacheKeyManager.AckCount}:Total", 1);
                                                    tran.IncrBy($"{cacheKeyManager.AckCount}:{time}", 1);
                                                    tran.Expire($"{cacheKeyManager.AckCount}:{time}", TimeSpan.FromHours(30));
                                                }

                                                tran.Exec();
                                            }
                                            else if (messageHandler.IsAcknowledged)
                                            {
                                                using var tran = redisClient.Multi();
                                                tran.IncrBy($"{cacheKeyManager.ConsumeSucceeded}:Total", 1);
                                                tran.IncrBy($"{cacheKeyManager.ConsumeSucceeded}:{time}", 1);
                                                tran.Expire($"{cacheKeyManager.ConsumeSucceeded}:{time}", TimeSpan.FromHours(30));
                                                tran.Exec();
                                            }
                                            else
                                            {
                                                logger?.LogWarning("Queue {queueName} message {messageId} completed without ack, keep processing until pending timeout", queueName, message.MessageId);
                                            }

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} consume {message.MessageContent} successfully");
                                        }
                                        catch (Exception ex)
                                        {
                                            try
                                            {
                                                //Execute message
                                                using var scope = serviceProvider.CreateScope();
                                                var consumer = (IRedisMQConsumerExecutor)scope.ServiceProvider.GetRequiredService(consumerType);
                                                await consumer.ExecuteErrorAsync(messageContent, messageHandler, ex, stoppingToken).ConfigureAwait(false);
                                            }
                                            catch (Exception errorEx)
                                            {
                                                logger?.LogError(new RedisMQDataException("Handle error exception!", message, errorEx), "Handle error exception!");
                                            }

                                            try
                                            {
                                                using var pipe = redisClient.StartPipe();
                                                pipe.IncrBy($"{cacheKeyManager.ConsumeFailed}:Total", 1);
                                                pipe.IncrBy($"{cacheKeyManager.ConsumeFailed}:{time}", 1);
                                                pipe.Expire($"{cacheKeyManager.ConsumeFailed}:{time}", TimeSpan.FromHours(30));

                                                if (message != null)
                                                {
                                                    pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                                    pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);
                                                    pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                                    
                                                    //超出重试次数, 从原来的队列中删除, 并加入到异常列表
                                                    var messageErrorInfo = default(MessageErrorInfo);
                                                    var errorInfoStr = redisClient.HGet<string>($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "ErrorInfo");
                                                    if (errorInfoStr != null)
                                                    {
                                                        messageErrorInfo = queueConfig.Serializer.Deserialize<MessageErrorInfo>(errorInfoStr)!;
                                                    }
                                                    else
                                                    {
                                                        messageErrorInfo = new MessageErrorInfo
                                                        {
                                                            ConsumerName = consumerName,
                                                            ExceptionMessage = ex.Message,
                                                            StackTrace = ex.StackTrace,
                                                            GroupName = groupName,
                                                            QueueName = queueName,
                                                            ErrorMessageContent = messageContent,
                                                            ErrorMessageTimestamp = TimeHelper.GetMillisecondTimestamp()
                                                        };
                                                    }

                                                    var executionTimes = redisClient.HGet<int>($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "ExecutionTimes");
                                                    //超出了重试次数, 则删除队列消息, 并添加到错误列表
                                                    if (executionTimes > automaticRetryAttempts)
                                                    {
                                                        pipe.XAck(queueNameKey, groupName, data.id);
                                                        pipe.XDel(queueNameKey, data.id);
                                                        pipe.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Failed, "ErrorInfo", queueConfig.Serializer.Serialize(messageErrorInfo));
                                                        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                                        pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                    }
                                                    else
                                                    {
                                                        pipe.HSet($"{cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Retrying, "ErrorInfo", queueConfig.Serializer.Serialize(messageErrorInfo));
                                                        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), message.MessageId);
                                                        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), message.MessageId);
                                                        pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                    }

                                                    pipe.EndPipe();
                                                }
                                            }
                                            catch (Exception errorEx)
                                            {
                                                logger?.LogError(new RedisMQDataException("Handle error data exception!", message, errorEx), "Handle error data exception!");
                                            }

                                            logger?.LogError(new RedisMQDataException("Handle consumer message exception!", message, ex), "Handle consumer message exception!");
                                        }
                                        finally
                                        {
                                            message = null;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, $"{curConsumerName} read message exception!");
                            await Task.Delay(30, stoppingToken);
                        }
                    }
                }, stoppingToken));
            }
        }

        return Task.WhenAll(tasks);
    }

    private async Task ExecuteDelayQueuePollingAsync(ConsumerExecutorDescriptor consumerExecutorDescriptor, CancellationToken stoppingToken)
    {
        var queueName = consumerExecutorDescriptor.QueueName;
        var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
        var queueNameKey = cacheKeyManager.GetQueueName(queueName);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delayTimeSortedSetName = $"{queueNameKey}:DelayTimeType";
            var delaySecondsSet = redisClient.SMembers<double>(delayTimeSortedSetName);
            if (delaySecondsSet != null && delaySecondsSet.Any())
            {
                var messageMemberBag = new ConcurrentBag<(string DelayQueueName, string Member, decimal Score)>();
                foreach (var delaySeconds in delaySecondsSet.AsParallel())
                {
                    var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";
                    var delayQueueLock = redisClient.Lock($"{delayQueueName}:PollingLock", 5);
                    if (delayQueueLock == null)
                        continue;
                    try
                    {
                        var zMembers = await redisClient.ZRangeByScoreWithScoresAsync(delayQueueName, 0, TimeHelper.GetMillisecondTimestamp(), 0, prefetchCount).ConfigureAwait(false);

                        foreach (var item in zMembers)
                        {
                            messageMemberBag.Add((delayQueueName, item.member, item.score));
                        }
                    }
                    finally
                    {
                        delayQueueLock.Unlock();
                    }
                }

                var messageMemberList = messageMemberBag.OrderBy(x => x.Score).ToList();
                if (messageMemberList.Any())
                {
                    foreach (var item in messageMemberList)
                    {
                        var baseMessage = queueConfig.Serializer.Deserialize<BaseMessage>(item.Member ?? "");
                        if (baseMessage == null)
                            continue;

                        if (redisClient.ZRem(item.DelayQueueName, item.Member) <= 0)
                            continue;

                        try
                        {
                            var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));
                            var messageId = await redisClient.XAddAsync(queueNameKey, data).ConfigureAwait(false);

                            await redisClient.HSetAsync($"{cacheKeyManager.PublishMessageList}:{baseMessage.MessageId}", "Id", messageId).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            redisClient.ZAdd(item.DelayQueueName, item.Score, item.Member);
                            logger.LogError(ex, "Queue {queueName} move delay message to stream failed, message restored to {delayQueueName}", queueName, item.DelayQueueName);
                        }
                    }

                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(3000, stoppingToken).ConfigureAwait(false);
                }
            }
            else
            {
                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private Task RunLiveServerAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await redisClient.HSetAsync(cacheKeyManager.ServerNodes, _serverName, DateTime.Now).ConfigureAwait(false);

                var serverNodes = await redisClient.HGetAllAsync<DateTime>(cacheKeyManager.ServerNodes).ConfigureAwait(false);
                var expireNodes = serverNodes.Where(x => x.Value < DateTime.Now.AddMinutes(-2));
                var consumerList = await redisClient.SMembersAsync(cacheKeyManager.ConsumerList).ConfigureAwait(false);
                var expireConsumerList = consumerList.Where(x => !serverNodes.Any(s => x.StartsWith($"{s.Key}:"))).ToList();
                if (expireNodes.Any())
                {
                    foreach (var node in expireNodes)
                    {
                        redisClient.HDel(cacheKeyManager.ServerNodes, node.Key);
                        expireConsumerList.AddRange(consumerList.Where(x => x.StartsWith($"{node.Key}:")));
                    }
                }

                if (expireConsumerList.Any())
                {
                    await redisClient.SRemAsync(cacheKeyManager.ConsumerList, expireConsumerList.Select(x => (object)x).ToArray()).ConfigureAwait(false);
                }

                await Task.Delay(20000, stoppingToken).ConfigureAwait(false);
            }
        }, stoppingToken);
    }
}
