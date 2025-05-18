using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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