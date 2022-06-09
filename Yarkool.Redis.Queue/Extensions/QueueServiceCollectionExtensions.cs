using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            ArgumentNullException.ThrowIfNull(queueConfig.RedisOptions, nameof(queueConfig.RedisOptions));
            ArgumentNullException.ThrowIfNull(queueConfig.RedisOptions.Host, nameof(queueConfig.RedisOptions.Host));

            services.AddSingleton(queueConfig);

            return services;
        }
    }
}
