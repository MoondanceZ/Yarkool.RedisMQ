using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Yarkool.RedisMQ;

namespace Yarkool.RedisMQ
{
    internal sealed class RedisMQStartupFilter(QueueConfig queueConfig) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                if (queueConfig.DashboardOptions != null)
                {
                    app.UseRedisMQDashboard();
                }
            };
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisMQOptionsExtensions
    {
        public static QueueConfig UseDashboard(this QueueConfig queueConfig)
        {
            return queueConfig.UseDashboard(opt => { });
        }

        public static QueueConfig UseDashboard(this QueueConfig queueConfig, Action<DashboardOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var dashboardOptions = new DashboardOptions();
            options.Invoke(dashboardOptions);

            queueConfig.DashboardOptions = dashboardOptions;

            return queueConfig;
        }
    }
}