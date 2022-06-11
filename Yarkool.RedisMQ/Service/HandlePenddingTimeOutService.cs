using FreeRedis;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 处理等待超时
    /// </summary>
    internal class HandlePenddingTimeOutService: BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var messageTypes = assemblies.SelectMany(x => x.GetTypes().Where(t => t.GetCustomAttributes(false).Any(p => p.GetType() == typeof(QueueAttribute)))).ToList();

            if (messageTypes.Any())
            {
                var queueConfig = IocContainer.Resolve<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
                var redisClient = IocContainer.Resolve<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));

                foreach (var item in messageTypes)
                {
                    var queueAttr = item.GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute ?? throw new ArgumentNullException(nameof(QueueAttribute));

                    var queueName = $"{queueConfig.RedisPrefix}{queueAttr.QueueName}";
                    var groupName = $"{queueAttr.QueueName}_Group";

                    redisClient.XPending(queueName, groupName, "-", "+", 10);
                }
            }

            return Task.CompletedTask;
        }
    }
}
