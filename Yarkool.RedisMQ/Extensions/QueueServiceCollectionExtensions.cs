using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            services.AddSingleton<ErrorPublisher>();

            if (!services.Any(x => x.ServiceType == typeof(ILoggerFactory)))
                services.AddLogging();

            if (queueConfig.AutoInitSubscriber)
                services.AddRedisMQSubscriber();

            if (queueConfig.AutoInitPublisher)
                services.AddRedisMQPublisher();

            if (queueConfig.AutoRePublishTimeOutMessage)
                services.AddHostedService<HandlePendingTimeOutService>();

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
            if (!services.Any(x => x.ServiceType == typeof(RedisClient)))
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
        private static IServiceCollection AddRedisMQSubscriber(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var subscriberTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(ISubscriber).IsAssignableFrom(t) && t.BaseType?.Name == nameof(BaseSubscriber))).ToList();

            foreach (var item in subscriberTypes)
            {
                services.AddHostedService(item);
            }

            //订阅者这里需要注入错误队列发布者
            services.AddSingleton<ErrorPublisher>();

            return services;
        }

        /// <summary>
        /// 注入Subscriber
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisMQSubscriber<TSubscriber>(this IServiceCollection services) where TSubscriber : class, ISubscriber, IHostedService
        {
            services.AddHostedService<TSubscriber>();

            return services;
        }

        /// <summary>
        /// 注入Publisher
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddRedisMQPublisher(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var publisherTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IPublisher).IsAssignableFrom(t) && t.BaseType?.Name == nameof(BasePublisher))).ToList();

            foreach (var item in publisherTypes)
            {
                if (item == typeof(ErrorPublisher))
                    continue;

                services.AddSingleton(item);
            }

            return services;
        }

        /// <summary>
        /// 注入Publisher
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddRedisMQPublisher<TPublisher>(this IServiceCollection services) where TPublisher : class, ISubscriber
        {
            services.AddSingleton<TPublisher>();

            return services;
        }

        private static IServiceCollection AddHostedService(this IServiceCollection services, Type type)
        {
            var method = typeof(ServiceCollectionHostedServiceExtensions)
                .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), new[] { typeof(IServiceCollection) })
                ?.MakeGenericMethod(type);
            method?.Invoke(null, new object[] { services });

            return services;
        }
    }
}