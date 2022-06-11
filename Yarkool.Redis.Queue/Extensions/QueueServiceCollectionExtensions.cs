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

            services.AddTransient<ErrorPublisher>();

            services.AddLogging();

            services.AddQueueSubscriber();

            services.AddQueuePublisher();

            var serviceProvider = services.BuildServiceProvider();
            IocContainer.Initialize(serviceProvider);

            InitializeSubscriber();

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
        /// 注入Subscriber
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddQueueSubscriber(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var subscriberTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(ISubscriber).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BaseSubscriber<>).Name)).ToList();

            foreach (var item in subscriberTypes)
            {
                services.AddTransient(item);
            }

            return services;
        }

        /// <summary>
        /// 注入Publisher
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddQueuePublisher(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var publisherTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IPublisher).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BasePublisher<>).Name)).ToList();

            foreach (var item in publisherTypes)
            {
                services.AddTransient(item);
            }


            services.addho

            return services;
        }

        /// <summary>
        /// 初始化消费者
        /// </summary>
        private static void InitializeSubscriber()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var subscriberTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(ISubscriber).IsAssignableFrom(t) && t.BaseType?.Name == typeof(BaseSubscriber<>).Name)).ToList();

            foreach (var item in subscriberTypes)
            {
                if (IocContainer.Resolve(item) is ISubscriber subscriber)
                {
                    Task.Run(() => subscriber.SubscribeAsync());
                }
            }
        }
    }
}