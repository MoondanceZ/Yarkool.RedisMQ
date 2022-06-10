using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeRedis;
using Microsoft.Extensions.Logging;

namespace Yarkool.Redis.Queue
{
    public static class QueueServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisQueue(this IServiceCollection services, Action<QueueConfig> config)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            var queueConfig = new QueueConfig();
            config(queueConfig);

            services.AddSingleton(queueConfig);

            services.AddTransient<ErrorProducer>();

            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            IocContainer.Initialize(serviceProvider);

            return services;
        }
        
        public static IServiceCollection AddRedisQueue(this IServiceCollection services, RedisClient redisClient, Action<QueueConfig> config)
        {
            services.AddSingleton(redisClient);

            services.AddRedisQueue(config);

            return services;
        }
    }
}