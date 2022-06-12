using FreeRedis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 处理等待超时
    /// </summary>
    internal class HandlependingTimeOutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HandlependingTimeOutService> _logger;

        public HandlependingTimeOutService(IServiceProvider serviceProvider, ILogger<HandlependingTimeOutService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var messageTypes = assemblies.SelectMany(x => x.GetTypes().Where(t => t.GetCustomAttributes(false).Any(p => p.GetType() == typeof(QueueAttribute)))).ToList();

                if (messageTypes.Any())
                {
                    var queueConfig = _serviceProvider.GetRequiredService<QueueConfig>();
                    var redisClient = _serviceProvider.GetRequiredService<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));
                    var serializer = queueConfig.Serializer;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        foreach (var messageType in messageTypes)
                        {
                            var queueAttr = messageType.GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute ?? throw new ArgumentNullException(nameof(QueueAttribute));

                            var queueName = $"{queueConfig.RedisPrefix}{queueAttr.QueueName}";
                            var groupName = $"{queueAttr.QueueName}_Group";
                            var subscriberName = $"{queueAttr.QueueName}_Subscriber";
                            var subscriberCount = queueAttr.SubscriberCount;

                            for (int i = 0; i < subscriberCount; i++)
                            {
                                var subscriberIndex = i + 1;
                                var pendingResults = redisClient.XReadGroup(groupName, $"{subscriberName}_{subscriberIndex}", 20, 0, false, queueName, "0-0");
                                if (pendingResults != null && pendingResults.FirstOrDefault()?.entries.Any() == true)
                                {
                                    foreach (var entry in pendingResults.FirstOrDefault()!.entries)
                                    {
                                        long.TryParse(entry.id.Split("-").FirstOrDefault(), out var messageTime);
                                        var isTimeOuntMessage = (TimeHelper.GetMillisecondTimestamp() - messageTime) > queueAttr.PendingTimeOut * 1000;
                                        if (isTimeOuntMessage)
                                        {
                                            var message = MapToClass(entry.fieldValues, messageType, encoding: Encoding.UTF8);
                                            if (message != null)
                                            {
                                                var data = serializer.Deserialize<Dictionary<string, string>>(serializer.Serialize(message));
                                                redisClient.XAdd(queueName, data);
                                            }
                                            redisClient.XAck(queueName, groupName, entry.id);
                                            redisClient.XDel(queueName, entry.id);
                                        }
                                    }
                                }
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handle Pending TimeOut Message Error!");
            }
        }

        private object? MapToClass(object[] list, Type type, Encoding encoding)
        {
            var method = typeof(FreeRedis.RespHelper).GetMethod(nameof(FreeRedis.RespHelper.MapToClass))?.MakeGenericMethod(new Type[] { type });
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
