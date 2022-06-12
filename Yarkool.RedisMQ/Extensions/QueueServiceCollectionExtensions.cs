using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeRedis;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ
{
    public static class QueueServiceCollectionExtensions
    {
        /// <summary>
        /// AddRedisMQ
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisMQ(this IServiceCollection services, Action<QueueConfig>? config = null)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            var queueConfig = new QueueConfig();
            config?.Invoke(queueConfig);

            services.AddSingleton(queueConfig);

            services.AddTransient<ErrorPublisher>();

            if (!services.Any(x => x.ServiceType ==  typeof(ILoggerFactory)))
                services.AddLogging();

            if (queueConfig.AutoInitSubscriber)
                services.AddQueueSubscriber();

            if (queueConfig.AutoInitPublisher)
                services.AddQueuePublisher();

            if (queueConfig.AutoRePublishTimeOutMessage)
                services.AddHostedService<HandlependingTimeOutService>();

            var serviceProvider = services.BuildServiceProvider();
            IocContainer.Initialize(serviceProvider);

            // InitializeSubscriber();

            return services;
        }

        /// <summary>
        /// AddRedisMQ
        /// </summary>
        /// <param name="services"></param>
        /// <param name="redisClient"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisMQ(this IServiceCollection services, RedisClient redisClient, Action<QueueConfig>? config = null)
        {
            services.AddSingleton(redisClient);

            services.AddRedisMQ(config);

            return services;
        }

        /// <summary>
        /// AddRedisMQ
        /// </summary>
        /// <param name="services"></param>
        /// <param name="redisConnStr"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisMQ(this IServiceCollection services, string redisConnStr, Action<QueueConfig>? config = null)
        {
            return services.AddRedisMQ(new RedisClient(redisConnStr), config);
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
                services.AddHostedService(item);
            }

            //订阅者这里需要注入错误队列发布者
            services.AddSingleton<ErrorPublisher>();

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
                if (item == typeof(ErrorPublisher))
                    continue;

                services.AddSingleton(item);
            }

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

        private static IServiceCollection AddHostedService(this IServiceCollection services, Type type)
        {
            var method = typeof(ServiceCollectionHostedServiceExtensions).GetMethods()
                .Where(x=> x.Name == nameof(ServiceCollectionHostedServiceExtensions.AddHostedService) && x.GetParameters().Length == 1).FirstOrDefault()?
                .MakeGenericMethod(new Type[] {type});
            method?.Invoke(null, new object[] {services});

            return services;
        }
    }
}