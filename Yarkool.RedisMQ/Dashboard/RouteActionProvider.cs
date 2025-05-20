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
    private readonly IRedisClient _redisClient = builder.ServiceProvider.GetService<IRedisClient>()!;
    private readonly CacheKeyManager _cacheKeyManager = builder.ServiceProvider.GetService<CacheKeyManager>()!;
    private readonly QueueConfig _queueConfig = builder.ServiceProvider.GetService<QueueConfig>()!;

    public void MapDashboardRoutes()
    {
        var prefixMatch = options.PathMatch + "/api";

        builder.MapGet(prefixMatch + "/stats", Stats).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapGet(prefixMatch + "/message/list", MessageList).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
    }

    private async Task Stats(HttpContext httpContext)
    {
        var twentyFourHoursStatsList = new List<StatsResponse.Types.TwentyFourHoursStatsInfo>();
        var now = DateTime.Now;
        for (int i = 0; i < 24; i++)
        {
            var time = now.AddHours(-i).ToString("yyyyMMddHH");
            var twentyFourHoursPipe = _redisClient.StartPipe();
            twentyFourHoursPipe.Get<long>($"{_cacheKeyManager.ConsumeFailed}:{time}");
            twentyFourHoursPipe.Get<long>($"{_cacheKeyManager.ConsumeSucceeded}:{time}");
            twentyFourHoursPipe.Get<long>($"{_cacheKeyManager.PublishFailed}:{time}");
            twentyFourHoursPipe.Get<long>($"{_cacheKeyManager.PublishSucceeded}:{time}");
            twentyFourHoursPipe.Get<long>($"{_cacheKeyManager.AckCount}:{time}");
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

        var realTimePipe = _redisClient.StartPipe();
        realTimePipe.Get<long>($"{_cacheKeyManager.ConsumeFailed}:Total"); //0
        realTimePipe.Get<long>($"{_cacheKeyManager.ConsumeSucceeded}:Total"); //1
        realTimePipe.Get<long>($"{_cacheKeyManager.PublishFailed}:Total"); //2
        realTimePipe.Get<long>($"{_cacheKeyManager.PublishSucceeded}:Total"); //3
        realTimePipe.Get<long>($"{_cacheKeyManager.AckCount}:Total"); //4
        realTimePipe.ZCard(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed)); //5
        realTimePipe.SCard(_cacheKeyManager.CommonQueueList); //6
        realTimePipe.SCard(_cacheKeyManager.DelayQueueList); //7
        realTimePipe.SCard(_cacheKeyManager.ConsumerList); //8
        realTimePipe.HLen(_cacheKeyManager.ServerNodes); //9
        realTimePipe.ZCard(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending)); //10
        realTimePipe.ZCard(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing)); //11
        realTimePipe.ZCard(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying)); //12
        realTimePipe.ZCard(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed)); //13
        var realTimeResults = realTimePipe.EndPipe();

        var result = new StatsResponse
        {
            RealTimeStats = new StatsResponse.Types.StatsInfo
            {
                ConsumeFailed = (long)realTimeResults[0],
                ConsumeSucceeded = (long)realTimeResults[1],
                PublishFailed = (long)realTimeResults[2],
                PublishSucceeded = (long)realTimeResults[3],
                AckCount = (long)realTimeResults[4],
                FailedCount = (long)realTimeResults[5],
                PendingCount = (long)realTimeResults[10],
                ProcessingCount = (long)realTimeResults[11],
                RetryingCount = (long)realTimeResults[12],
                CompletedCount = (long)realTimeResults[13]
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

    private async Task MessageList(HttpContext httpContext)
    {
        var redisClient = _serviceProvider.GetService<IRedisClient>()!;

        var pageRequest = httpContext.Request.Query.ToObject<MessagePageRequest>();

        var pageIndex = pageRequest.PageIndex;
        var pageSize = pageRequest.PageSize;
        var status = pageRequest.Status;
        var key = status.HasValue ? _cacheKeyManager.GetStatusMessageIdSet(status.Value) : _cacheKeyManager.PublishMessageIdSet;

        var start = (pageIndex - 1) * pageSize;
        var stop = start + pageSize - 1;

        var result = new List<MessageResponse>();
        var data = redisClient.ZRevRange(key, start, stop);
        if (data != null)
        {
            using var pipe = redisClient.StartPipe();
            foreach (var item in data)
            {
                pipe.HGetAll<string>($"{_cacheKeyManager.PublishMessageList}:{item}");
            }

            var pipeResult = pipe.EndPipe();

            foreach (var obj in pipeResult)
            {
                var dic = obj as IDictionary<string, string>;
                result.Add(new MessageResponse
                {
                    Message = _queueConfig.Serializer.Deserialize<BaseMessage>(dic!["Message"])!,
                    Status = Enum.Parse<MessageStatus>(dic["Status"]),
                    ErrorInfo = dic.TryGetValue("ErrorInfo", out string? value) ? _queueConfig.Serializer.Deserialize<MessageErrorInfo>(value) : null,
                });
            }
        }

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