using FreeRedis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ;

internal class RouteActionProvider
(
    IEndpointRouteBuilder builder,
    DashboardOptions options,
    CacheKeyManager cacheKeyManager
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

        var twentyFourHoursStatsList = new List<StatsResponse.Types.TwentyFourHoursStatsInfo>();
        var now = DateTime.Now;
        for (int i = 0; i < 24; i++)
        {
            var time = now.AddHours(-i).ToString("yyyyMMddHH");
            var twentyFourHoursPipe = redisClient.StartPipe();
            twentyFourHoursPipe.Get<long>($"{cacheKeyManager.ConsumeFailed}:{time}");
            twentyFourHoursPipe.Get<long>($"{cacheKeyManager.ConsumeSucceeded}:{time}");
            twentyFourHoursPipe.Get<long>($"{cacheKeyManager.PublishFailed}:{time}");
            twentyFourHoursPipe.Get<long>($"{cacheKeyManager.PublishSucceeded}:{time}");
            twentyFourHoursPipe.Get<long>($"{cacheKeyManager.AckCount}:{time}");
            var twentyFourHoursResults = twentyFourHoursPipe.EndPipe();

            var statsInfo = new StatsResponse.Types.StatsInfo
            {
                ConsumeFailed = (long)twentyFourHoursResults[0],
                ConsumeSucceeded = (long)twentyFourHoursResults[1],
                PublishFailed = (long)twentyFourHoursResults[2],
                PublishSucceeded = (long)twentyFourHoursResults[3],
                AckCount = (long)twentyFourHoursResults[4]
            };
            twentyFourHoursStatsList.Add(new StatsResponse.Types.TwentyFourHoursStatsInfo
            {
                Time = now.AddHours(-i).ToString("MM-dd HH:00"),
                Stats = statsInfo
            });
        }

        twentyFourHoursStatsList.Reverse();

        var realTimePipe = redisClient.StartPipe();
        realTimePipe.Get<long>($"{cacheKeyManager.ConsumeFailed}:Total"); //0
        realTimePipe.Get<long>($"{cacheKeyManager.ConsumeSucceeded}:Total"); //1
        realTimePipe.Get<long>($"{cacheKeyManager.PublishFailed}:Total"); //2
        realTimePipe.Get<long>($"{cacheKeyManager.PublishSucceeded}:Total"); //3
        realTimePipe.Get<long>($"{cacheKeyManager.AckCount}:Total"); //4
        realTimePipe.XLen(cacheKeyManager.ParseCacheKey(queueConfig.ErrorQueueOptions?.QueueName ?? "")); //5
        realTimePipe.SCard(cacheKeyManager.CommonQueueList); //6
        realTimePipe.SCard(cacheKeyManager.DelayQueueList); //7
        realTimePipe.SCard(cacheKeyManager.ConsumerList); //8
        realTimePipe.HLen(cacheKeyManager.ServerNodes); //9
        var realTimeResults = realTimePipe.EndPipe();

        var commonQueueList = redisClient.SMembers(cacheKeyManager.CommonQueueList);
        var delayQueueNameList = redisClient.SMembers(cacheKeyManager.DelayQueueNameList);
        var pendingCount = commonQueueList.Select(x => redisClient.XLen(cacheKeyManager.ParseCacheKey(x))).Sum() +
            delayQueueNameList.Select(x => redisClient.ZCard(x)).Sum();

        var result = new StatsResponse
        {
            RealTimeStats = new StatsResponse.Types.StatsInfo
            {
                ConsumeFailed = (long)realTimeResults[0],
                ConsumeSucceeded = (long)realTimeResults[1],
                PublishFailed = (long)realTimeResults[2],
                PublishSucceeded = (long)realTimeResults[3],
                AckCount = (long)realTimeResults[4],
                ErrorQueueLength = (long)realTimeResults[5],
                PendingCount = pendingCount
            },
            TwentyFourHoursStats = twentyFourHoursStatsList,
            ServerInfo = new StatsResponse.Types.ServerInfo
            {
                MessageCount = (long)realTimeResults[3],
                QueueCount = (long)realTimeResults[6] + (long)realTimeResults[7],
                ConsumerCount = (long)realTimeResults[8],
                ServerCount = (long)realTimeResults[9]
            }
        };

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(result));
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