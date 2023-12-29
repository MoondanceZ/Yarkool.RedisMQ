using FreeRedis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarkool.RedisMQ.Dashboard.Client.Pages;
using Yarkool.RedisMQ.Dashboard.Components;

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

            if (queueConfig.UseDashboard)
            {
                services.AddRazorComponents()
                    .AddInteractiveServerComponents()
                    .AddInteractiveWebAssemblyComponents();

                services.AddMasaBlazor();
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
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRedisMQConsumer<>)))
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
                new[]
                {
                    typeof(IServiceCollection)
                })?.MakeGenericMethod(type);
            method?.Invoke(null, new object[]
            {
                services
            });

            return services;
        }
        
        public static IApplicationBuilder UseRedisMQDashboard(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            
            var pathPrefix = "/redis-mq";

            app.Map(pathPrefix, subApp =>
            {
                //https://learn.microsoft.com/zh-cn/aspnet/core/blazor/host-and-deploy/?view=aspnetcore-8.0&tabs=visual-studio#app-base-path
                //https://learn.microsoft.com/zh-cn/aspnet/core/blazor/host-and-deploy/multiple-hosted-webassembly?view=aspnetcore-7.0&source=recommendations&pivots=port-domain
                //https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/routing?view=aspnetcore-8.0
                //均可以单独配置
                // Configure the HTTP request pipeline.
                var webApplication = app as WebApplication;
                if (webApplication?.Environment.IsDevelopment() == true)
                {
                    subApp.UseWebAssemblyDebugging();
                }
                else
                {
                    subApp.UseExceptionHandler("/Error", createScopeForErrors: true);
                }

                subApp.UsePathBase(pathPrefix);
                subApp.UseRouting();
                subApp.UseStaticFiles();
                subApp.UseBlazorFrameworkFiles();
                subApp.UseAntiforgery();

                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub(pathPrefix);
                    endpoints.MapRazorComponents<App>()
                        .AddInteractiveServerRenderMode()
                        .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = pathPrefix)
                        .AddAdditionalAssemblies(typeof(Counter).Assembly);
                });
            });

            return app;
        }
    }
}