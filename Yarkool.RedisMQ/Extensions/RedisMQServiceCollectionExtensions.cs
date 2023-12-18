using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ
{
    public static class RedisMQServiceCollectionExtensions
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

            if (!services.Any(x => x.ServiceType == typeof(ILoggerFactory)))
                services.AddLogging();

            if (queueConfig.UseErrorQueue)
                services.AddSingleton<ErrorPublisher>();

            if (queueConfig.AutoInitSubscriber)
                services.AddRedisMQSubscriber();

            if (queueConfig.AutoInitPublisher)
                services.AddRedisMQPublisher();

            // if (queueConfig.AutoRePublishTimeOutMessage)
            //     services.AddHostedService<HandlePendingTimeOutService>();

            services.AddHostedService<SubscriberBackgroundService>();

            services.AddSingleton<ConsumerExecutorDescriptor>();

            var serviceProvider = services.BuildServiceProvider();
            IocContainer.Initialize(serviceProvider);

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
            var basePublisherGenericType = typeof(BaseConsumer<>);
            var subscriberTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == basePublisherGenericType)
                .ToList();

            foreach (var item in subscriberTypes)
            {
                if (item == typeof(ErrorPublisher))
                    continue;

                services.AddTransient(item);
            }

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
            var basePublisherGenericType = typeof(BasePublisher<>);
            var publisherTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == basePublisherGenericType)
                .ToList();

            foreach (var item in publisherTypes)
            {
                if (item == typeof(ErrorPublisher))
                    continue;

                services.AddSingleton(typeof(IConsumer), item);
            }

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