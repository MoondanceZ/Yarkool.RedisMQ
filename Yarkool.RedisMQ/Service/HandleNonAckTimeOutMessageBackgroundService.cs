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
    internal class HandlePendingTimeOutService : BackgroundService
    {
        private readonly ConsumerServiceSelector _consumerServiceSelector;
        private readonly QueueConfig _queueConfig;
        private readonly CacheKeyManager _cacheKeyManager;
        private readonly IRedisClient _redisClient;
        private readonly ILogger<HandlePendingTimeOutService> _logger;

        public HandlePendingTimeOutService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, CacheKeyManager cacheKeyManager, IRedisClient redisClient, ILogger<HandlePendingTimeOutService> logger)
        {
            _consumerServiceSelector = consumerServiceSelector;
            _queueConfig = queueConfig;
            _cacheKeyManager = cacheKeyManager;
            _redisClient = redisClient;
            _logger = logger;

            foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
            {
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;
                var queueNameKey = cacheKeyManager.GetQueueName(queueName);

                //初始化队列信息
                if (!_redisClient.Exists(queueNameKey))
                {
                    _redisClient.XGroupCreate(queueNameKey, groupName, MkStream: true);
                }
                else
                {
                    var infoGroups = _redisClient.XInfoGroups(queueNameKey);
                    if (!infoGroups.Any(x => x.name == groupName))
                        _redisClient.XGroupCreate(queueNameKey, groupName, MkStream: true);
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
            {
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;
                var automaticRetryAttempts = consumerExecutorDescriptor.AutomaticRetryAttempts;
                var pendingTimeOut = consumerExecutorDescriptor.PendingTimeOut * 1000;
                var queueNameKey = _cacheKeyManager.GetQueueName(queueName);

                Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var readPendingLock = _redisClient.Lock($"{_cacheKeyManager.ReadPendingLock}:{queueName}", 30);
                            if (readPendingLock == null)
                                continue;
                            try
                            {
                                var timeOutMessageIdTimestamp = TimeHelper.GetMillisecondTimestamp() - pendingTimeOut;
                                var timeOutPendingResults = await _redisClient.XPendingAsync(queueNameKey, groupName, "0-0", $"{timeOutMessageIdTimestamp}-0", 50).ConfigureAwait(false);
                                if (timeOutPendingResults != null && timeOutPendingResults.Length != 0)
                                {
                                    foreach (var result in timeOutPendingResults)
                                    {
                                        var messageId = result.id;
                                        var messageRange = await _redisClient.XRangeAsync(queueNameKey, messageId, messageId).ConfigureAwait(false);
                                        if (messageRange is not { Length: > 0 })
                                            continue;
                                        var entry = messageRange[0];
                                        var message = entry.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);
                                        if (message != null)
                                        {
                                            // 再判一次是否超时
                                            var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - message.CreateTimestamp > pendingTimeOut;
                                            if (isTimeOutMessage)
                                            {
                                                message.CreateTimestamp = TimeHelper.GetMillisecondTimestamp();

                                                //分为2种情况, 普通的未正常ACK超时, 一种是错误超时
                                                //错误超时
                                                var messageErrorInfo = default(MessageErrorInfo);
                                                var errorInfoStr = _redisClient.HGet<string>($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "ErrorInfo");
                                                if (errorInfoStr != null)
                                                    messageErrorInfo = _queueConfig.Serializer.Deserialize<MessageErrorInfo>(errorInfoStr);

                                                if (messageErrorInfo != null)
                                                {
                                                    var executionTimes = _redisClient.HGet<int>($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "ExecutionTimes");
                                                    //超出了重试次数, 则删除队列消息, 并添加到错误列表
                                                    if (executionTimes > automaticRetryAttempts)
                                                    {
                                                        using var pipeError = _redisClient.StartPipe();
                                                        pipeError.XAck(queueNameKey, groupName, entry.id);
                                                        pipeError.XDel(queueNameKey, entry.id);
                                                        pipeError.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                        pipeError.ZAdd(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), TimeHelper.GetMillisecondTimestamp(), _queueConfig.Serializer.Serialize(message));
                                                        pipeError.EndPipe();
                                                        continue;
                                                    }
                                                }

                                                var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(message));

                                                using var pipe = _redisClient.StartPipe();
                                                pipe.XAck(queueNameKey, groupName, entry.id);
                                                pipe.XDel(queueNameKey, entry.id);
                                                pipe.XAdd(queueNameKey, data);
                                                var res = pipe.EndPipe();
                                                await _redisClient.HSetAsync($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "Id", res[2].ToString());

                                                _logger?.LogInformation("Queue {queueName} republish pending timeout message {content}", queueName, message.MessageContent);
                                            }
                                        }
                                        else
                                        {
                                            long.TryParse(messageRange[0].id.Split("-").FirstOrDefault(), out var messageTime);
                                            var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - messageTime > pendingTimeOut;
                                            if (isTimeOutMessage)
                                            {
                                                using var pipe = _redisClient.StartPipe();
                                                pipe.XAck(queueNameKey, groupName, entry.id);
                                                pipe.XDel(queueNameKey, entry.id);
                                                pipe.EndPipe();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Handle timeout data exception!");
                            }
                            finally
                            {
                                readPendingLock.Unlock();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Handle timeout lock exception!");
                        }
                    }
                }, stoppingToken);
            }

            return Task.CompletedTask;
        }
    }
}