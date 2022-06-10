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
        /// <summary>
        /// AddRedisQueue
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisQueue(this IServiceCollection services, Action<QueueConfig> config)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            var queueConfig = new QueueConfig();
            config(queueConfig);

            services.AddSingleton(queueConfig);

            services.AddTransient<ErrorProducer>();

            services.AddLogging();

            services.AddQueueConsumer();

            services.AddQueueProducer();

            var serviceProvider = services.BuildServiceProvider();
            IocContainer.Initialize(serviceProvider);

            InitializeConsumer();

            return services;
        }

        /// <summary>
        /// AddRedisQueue
        /// </summary>
        /// <param name="services"></param>
        /// <param name="redisClient"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisQueue(this IServiceCollection services, RedisClient redisClient, Action<QueueConfig> config)
        {
            services.AddSingleton(redisClient);

            services.AddRedisQueue(config);

            return services;
        }

        /// <summary>
        /// 注入Consumer
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddQueueConsumer(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var consumerTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IConsumer).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BaseConsumer<>).Name)).ToList();

            foreach (var item in consumerTypes)
            {
                services.AddTransient(item);
            }

            return services;
        }

        /// <summary>
        /// 注入Producer
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddQueueProducer(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var producerTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IProducer).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BaseProducer<>).Name)).ToList();

            foreach (var item in producerTypes)
            {
                services.AddTransient(item);
            }

            return services;
        }

        /// <summary>
        /// 初始化消费者
        /// </summary>
        private static void InitializeConsumer()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var consumerTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IConsumer).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BaseConsumer<>).Name)).ToList();

            foreach (var item in consumerTypes)
            {
                if (IocContainer.Resolve(item) is IConsumer consumer)
                {
                    consumer.Subscribe();
                }
            }
        }
    }
}