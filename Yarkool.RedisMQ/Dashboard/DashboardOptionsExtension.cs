using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Yarkool.RedisMQ;
using Yarkool.RedisMQ.Internal;

namespace Yarkool.RedisMQ
{
    internal sealed class DashboardOptionsExtension : IRedisMQOptionsExtension
    {
        private readonly Action<DashboardOptions> _options;

        public DashboardOptionsExtension(Action<DashboardOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var dashboardOptions = new DashboardOptions();
            _options?.Invoke(dashboardOptions);
            services.AddTransient<IStartupFilter, CapStartupFilter>();
            services.AddSingleton(dashboardOptions);
        }
    }

    internal sealed class CapStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                app.UseCapDashboard();
            };
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static QueueConfig UseDashboard(this QueueConfig queueConfig)
        {
            return queueConfig.UseDashboard(opt => { });
        }

        public static QueueConfig UseDashboard(this QueueConfig capOptions, Action<DashboardOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return capOptions;
        }
    }
}