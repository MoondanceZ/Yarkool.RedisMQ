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
        private readonly IRedisClient _redisClient;
        private readonly ILogger<HandlePendingTimeOutService> _logger;

        public HandlePendingTimeOutService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, IRedisClient redisClient, ILogger<HandlePendingTimeOutService> logger)
        {
            _consumerServiceSelector = consumerServiceSelector;
            _queueConfig = queueConfig;
            _redisClient = redisClient;
            _logger = logger;

            foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
            {
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;

                //初始化队列信息
                if (!_redisClient.Exists(queueName))
                {
                    _redisClient.XGroupCreate(queueName, groupName, MkStream: true);
                }
                else
                {
                    var infoGroups = _redisClient.XInfoGroups(queueName);
                    if (!infoGroups.Any(x => x.name == groupName))
                        _redisClient.XGroupCreate(queueName, groupName, MkStream: true);
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
            {
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;
                var consumerName = $"{consumerExecutorDescriptor.QueueName}_Consumer";
                var pendingTimeOut = consumerExecutorDescriptor.PendingTimeOut * 1000;

                for (var i = 0; i < consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount; i++)
                {
                    var consumerIndex = i + 1;
                    var curConsumerName = $"{consumerName}_{consumerIndex}";
                    Task.Run(async () =>
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                var timeOutMessageIdTimestamp = TimeHelper.GetMillisecondTimestamp() - pendingTimeOut;
                                var timeOutPendingResults = await _redisClient.XPendingAsync(queueName, groupName, "0-0", $"{timeOutMessageIdTimestamp}-0", 50, curConsumerName).ConfigureAwait(false);
                                if (timeOutPendingResults != null && timeOutPendingResults.Length != 0)
                                {
                                    foreach (var result in timeOutPendingResults.AsParallel())
                                    {
                                        var messageId = result.id;
                                        var messageRange = await _redisClient.XRangeAsync(queueName, messageId, messageId).ConfigureAwait(false);
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
                                                var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(message));

                                                using var tran = _redisClient.Multi();
                                                _redisClient.XAck(queueName, groupName, entry.id);
                                                _redisClient.XDel(queueName, entry.id);
                                                tran.XAdd(queueName, data);
                                                var res = tran.Exec();
                                                await _redisClient.HSetAsync(CacheKeys.MessageIdMapping, message.MessageId, res[0].ToString());

                                                _logger?.LogInformation("Queue {queueName} republish pending timeout message {content}", queueName, message.MessageContent);
                                            }
                                        }
                                        else
                                        {
                                            long.TryParse(messageRange[0].id.Split("-").FirstOrDefault(), out var messageTime);
                                            var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - messageTime > pendingTimeOut;
                                            if (isTimeOutMessage)
                                            {
                                                using var tran = _redisClient.Multi();
                                                _redisClient.XAck(queueName, groupName, entry.id);
                                                _redisClient.XDel(queueName, entry.id);
                                                tran.Exec();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    await Task.Delay(5000, stoppingToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Handle non ack data exception!");
                            }
                        }
                    }, stoppingToken);
                }
            }

            return Task.CompletedTask;
        }
    }
}