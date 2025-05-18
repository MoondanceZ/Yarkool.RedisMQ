using FreeRedis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ;

internal class RouteActionProvider
(
    IEndpointRouteBuilder builder,
    DashboardOptions options
)
{
    private readonly IServiceProvider _serviceProvider = builder.ServiceProvider;

    public void MapDashboardRoutes()
    {
        var prefixMatch = options.PathMatch + "/api";

        builder.MapGet(prefixMatch + "/stats", Stats).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
    }

    private async Task Stats(HttpContext httpContext)
    {
        var redisClient = _serviceProvider.GetService<IRedisClient>()!;
        var queueConfig = _serviceProvider.GetService<QueueConfig>()!;
        var result = new StatisticsResponse
        {
            ConsumeFailed = redisClient.Get<long>(CacheKeys.ConsumeFailed),
            ConsumeSucceeded = redisClient.Get<long>(CacheKeys.ConsumeSucceeded),
            PublishFailed = redisClient.Get<long>(CacheKeys.PublishFailed),
            PublishSucceeded = redisClient.Get<long>(CacheKeys.PublishSucceeded),
            AckCount = redisClient.Get<long>(CacheKeys.AckCount),
            ErrorQueueLength = !string.IsNullOrEmpty(queueConfig.ErrorQueueOptions?.QueueName) ? redisClient.XLen(queueConfig.ErrorQueueOptions?.QueueName) : 0
        };
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    public Task Health(HttpContext httpContext)
    {
        httpContext.Response.WriteAsync("OK");
        return Task.CompletedTask;
    }

    private void BadRequest(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
}