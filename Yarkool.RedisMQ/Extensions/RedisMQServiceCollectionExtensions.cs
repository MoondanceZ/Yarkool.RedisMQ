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

            if (queueConfig.UseErrorQueue && string.IsNullOrEmpty(queueConfig.ErrorQueueName))
                throw new RedisMQException("error queue name cannot be empty!");

            services.AddRedisMQConsumer();
            services.AddSingleton<IRedisMQPublisher, RedisMQPublisher>();
            services.AddSingleton<ConsumerServiceSelector>();

            if (queueConfig.RegisterConsumerService)
            {
                services.AddHostedService<ConsumerBackgroundService>();
                if (queueConfig.RepublishNonAckTimeOutMessage)
                    services.AddHostedService<HandlePendingTimeOutService>();
            }

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
        public static IServiceCollection AddRedisMQ(this IServiceCollection services, IRedisClient redisClient, Action<QueueConfig>? config = null)
        {
            if (!services.Any(x => x.ServiceType == typeof(IRedisClient)))
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
        /// 注入Consumer
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisMQConsumer(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var consumerTypes = assemblies.SelectMany(x => x.GetTypes())
                .Where(t => t is { IsAbstract: false, IsClass: true, BaseType.IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(RedisMQConsumer<>))
                .ToList();

            foreach (var item in consumerTypes)
            {
                services.AddTransient(item);
            }

            return services;
        }

        private static IServiceCollection AddHostedService(this IServiceCollection services, Type type)
        {
            var method = typeof(ServiceCollectionHostedServiceExtensions).GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService),
                [typeof(IServiceCollection)])?.MakeGenericMethod(type);
            method?.Invoke(null, [services]);

            return services;
        }
    }
}