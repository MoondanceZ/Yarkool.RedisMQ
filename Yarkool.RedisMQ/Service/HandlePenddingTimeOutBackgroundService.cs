using System.Text;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly RedisClient _redisClient;
        private readonly ErrorPublisher _errorPublisher;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HandlePendingTimeOutService> _logger;

        public HandlePendingTimeOutService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, RedisClient redisClient, ErrorPublisher errorPublisher, IServiceProvider serviceProvider, ILogger<HandlePendingTimeOutService> logger)
        {
            _consumerServiceSelector = consumerServiceSelector;
            _queueConfig = queueConfig;
            _redisClient = redisClient;
            _errorPublisher = errorPublisher;
            _serviceProvider = serviceProvider;
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
                var consumerType = consumerExecutorDescriptor.ConsumerTypeInfo;
                var messageType = consumerExecutorDescriptor.MessageTypeInfo;
                var queueName = consumerExecutorDescriptor.QueueName;
                var groupName = consumerExecutorDescriptor.GroupName;
                var consumerName = $"{consumerExecutorDescriptor.QueueName}_Consumer";

                for (var i = 0; i < consumerExecutorDescriptor.QueueConsumerAttribute.ConsumerCount; i++)
                {
                    var consumerIndex = i + 1;
                    Task.Run(async () =>
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            // 0-0：标识读取已分配给当前 consumer ，但是还没经过 xack 指令确认的消息
                            var pendingResults = await _redisClient.XReadGroupAsync(groupName, $"{consumerName}_{consumerIndex}", 20, 5 * 1000, false, queueName, "0-0");
                            if (pendingResults != null && pendingResults.FirstOrDefault()?.entries.Any() == true)
                            {
                                foreach (var entry in pendingResults.FirstOrDefault()!.entries)
                                {
                                    long.TryParse(entry.id.Split("-").FirstOrDefault(), out var messageTime);
                                    var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - messageTime > consumerExecutorDescriptor.QueueConsumerAttribute.PendingTimeOut * 1000;
                                    if (isTimeOutMessage)
                                    {
                                        var message = MapToClass(entry.fieldValues, typeof(BaseMessage), Encoding.UTF8);
                                        if (message != null)
                                        {
                                            var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(message));
                                            await _redisClient.XAddAsync(queueName, data);
                                        }

                                        await _redisClient.XAckAsync(queueName, groupName, entry.id);
                                        await _redisClient.XDelAsync(queueName, entry.id);
                                    }
                                }
                            }
                        }
                    }, stoppingToken);
                }
            }

            return Task.CompletedTask;
        }

        private object? MapToClass(object[] list, Type type, Encoding encoding)
        {
            var method = typeof(RespHelper).GetMethod(nameof(RespHelper.MapToClass))?.MakeGenericMethod(type);
            return method?.Invoke(null, new object[]
            {
                list,
                encoding
            });
        }

        //private StreamsXPendingConsumerResult[] GetTimeOutPendingResults(RedisClient redisClient, string queueName, string groupName, string start, int pendingTimeOut)
        //{
        //    var list = new List<StreamsXPendingConsumerResult>();
        //    var pendingResults = redisClient.XPending(queueName, groupName, start, "+", 50);
        //    if (pendingResults != null && pendingResults.Any())
        //    {
        //        var timeOutResults = pendingResults.Where(x => x.idle > pendingTimeOut * 1000).ToList();
        //        list.AddRange(timeOutResults);

        //        var lastTimeOutResult = timeOutResults.LastOrDefault(x => x.idle > pendingTimeOut * 1000);
        //        if (lastTimeOutResult != null)
        //        {
        //            list.AddRange(GetTimeOutPendingResults(redisClient, queueName, groupName, lastTimeOutResult.id, pendingTimeOut));
        //        }
        //    }

        //    return list.ToArray();
        //}
    }
}