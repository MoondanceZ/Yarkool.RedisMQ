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
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;
        private readonly ISerializer _serializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HandlePendingTimeOutService> _logger;
        private readonly Dictionary<Type, QueueConsumerAttribute> _queueSubscriberDic = new();

        public HandlePendingTimeOutService(QueueConfig queueConfig, RedisClient redisClient, IServiceProvider serviceProvider, ILogger<HandlePendingTimeOutService> logger)
        {
            _queueConfig = queueConfig ?? throw new ArgumentNullException(nameof(queueConfig));
            _serializer = _queueConfig.Serializer;
            _redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
            _serviceProvider = serviceProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var subscriberServices = _serviceProvider.GetServices(typeof(IConsumer));
            foreach (var subscriber in subscriberServices)
            {
                var subscriberType = subscriber!.GetType();
                var queueSubscriberAttribute = subscriberType.GetCustomAttributes(typeof(QueueConsumerAttribute), false).FirstOrDefault() as QueueConsumerAttribute;
                ArgumentNullException.ThrowIfNull(queueSubscriberAttribute, nameof(QueueConsumerAttribute));

                _queueSubscriberDic.Add(subscriberType, queueSubscriberAttribute);

                var queueName = $"{_queueConfig.RedisPrefix}{queueSubscriberAttribute.QueueName}";
                var groupName = $"{queueSubscriberAttribute.QueueName}_Group";
                var subscriberName = $"{queueSubscriberAttribute.QueueName}_Subscriber";

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                for (var i = 0; i < _queueSubscriberDic.Count; i++)
                {
                    var subscriberIndex = i + 1;

                    var queueSubscriberItem = _queueSubscriberDic.ElementAt(i);
                    var queueSubscriberAttribute = queueSubscriberItem.Value;

                    var queueName = $"{_queueConfig.RedisPrefix}{queueSubscriberAttribute.QueueName}";
                    var groupName = $"{queueSubscriberAttribute.QueueName}_Group";
                    var subscriberName = $"{queueSubscriberAttribute.QueueName}_Subscriber";

                    var actualQueueName = string.IsNullOrEmpty(_queueConfig.RedisPrefix) ? queueName : queueName.Replace(_queueConfig.RedisPrefix, "");
                    _logger.LogInformation($"{actualQueueName} {subscriberName}_{subscriberIndex} subscribing");

                    var pendingResults = await _redisClient.XReadGroupAsync(groupName, $"{subscriberName}_{subscriberIndex}", 20, 0, false, queueName, "0-0");
                    if (pendingResults != null && pendingResults.FirstOrDefault()?.entries.Any() == true)
                    {
                        foreach (var entry in pendingResults.FirstOrDefault()!.entries)
                        {
                            long.TryParse(entry.id.Split("-").FirstOrDefault(), out var messageTime);
                            var isTimeOutMessage = TimeHelper.GetMillisecondTimestamp() - messageTime > queueSubscriberAttribute.PendingTimeOut * 1000;
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
            }
        }

        private object? MapToClass(object[] list, Type type, Encoding encoding)
        {
            var method = typeof(RespHelper).GetMethod(nameof(RespHelper.MapToClass))?.MakeGenericMethod(type);
            return method?.Invoke(null, new object[] { list, encoding });
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