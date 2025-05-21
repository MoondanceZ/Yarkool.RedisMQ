using System.Reflection;
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
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public void MapDashboardRoutes()
    {
        var prefixMatch = options.PathMatch + "/api";

        builder.MapGet(prefixMatch + "/stats", Stats).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapGet(prefixMatch + "/message/list", MessageList).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapPost(prefixMatch + "/message/delete", MessageDelete).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapGet(prefixMatch + "/queue/list", MessageList).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapGet(prefixMatch + "/consumer/list", QueueList).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
        builder.MapGet(prefixMatch + "/server/list", ServerList).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);
    }

    #region Stats
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
        realTimePipe.ZCard(_cacheKeyManager.PublishMessageIdSet); //14
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
                CompletedCount = (long)realTimeResults[13],
                AllCount = (long)realTimeResults[14],
            },
            TwentyFourHoursStats = twentyFourHoursStatsList,
            ServerInfo = new StatsResponse.Types.ServerInfo
            {
                MessageCount = (long)realTimeResults[3],
                QueueCount = (long)realTimeResults[6] + (long)realTimeResults[7],
                ConsumerCount = (long)realTimeResults[8],
                ServerCount = (long)realTimeResults[9],
                ServerTimestamp = TimeHelper.GetMillisecondTimestamp(),
                RedisVersion = _redisClient.Info("server").Split("\r\n").FirstOrDefault(x => x.StartsWith("redis_version:"))?.Replace("redis_version:", "") ?? "unknown",
                RedisMQVersion = (_assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown").Split("+").FirstOrDefault()
            }
        };

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(null, result));
    }
    #endregion

    #region MessageList
    private async Task MessageList(HttpContext httpContext)
    {
        var pageRequest = httpContext.Request.Query.ToObject<MessagePageRequest>();

        var pageIndex = pageRequest.PageIndex;
        var pageSize = pageRequest.PageSize;
        var status = pageRequest.Status;
        var key = status.HasValue ? _cacheKeyManager.GetStatusMessageIdSet(Enum.Parse<MessageStatus>(status.Value.ToString())) : _cacheKeyManager.PublishMessageIdSet;

        var start = (pageIndex - 1) * pageSize;
        var stop = start + pageSize - 1;

        var result = new List<MessageResponse>();
        var data = _redisClient.ZRevRange(key, start, stop);
        if (data != null)
        {
            using var pipe = _redisClient.StartPipe();
            foreach (var item in data)
            {
                pipe.HGetAll<string>($"{_cacheKeyManager.PublishMessageList}:{item}");
            }

            var pipeResult = pipe.EndPipe();

            foreach (var obj in pipeResult)
            {
                var dic = obj as Dictionary<string, string>;
                result.Add(new MessageResponse
                {
                    Message = _queueConfig.Serializer.Deserialize<BaseMessage>(dic!["Message"])!,
                    Status = Enum.Parse<MessageStatus>(dic["Status"]),
                    ExecutionTimes = int.Parse(dic["ExecutionTimes"]),
                    ErrorInfo = dic.TryGetValue("ErrorInfo", out string? value) ? _queueConfig.Serializer.Deserialize<MessageErrorInfo>(value) : null,
                });
            }
        }

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(null, result));
    }
    #endregion

    #region MessageDelete
    private async Task MessageDelete(HttpContext httpContext)
    {
        var idList = httpContext.Request.Body.ToObject<List<string>>();

        if (idList.Any())
        {
            using var pipe = _redisClient.StartPipe();
            foreach (var id in idList)
            {
                pipe.ZRem(_cacheKeyManager.PublishMessageIdSet, id);
                pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), id);
                pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), id);
                pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), id);
                pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), id);
                pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), id);
                pipe.Del($"{_cacheKeyManager.PublishMessageList}:{id}");
            }

            pipe.EndPipe();
            await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success("删除成功"));
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success("请选择要删除的消息"));
        }
    }
    #endregion

    #region QueueList
    private async Task QueueList(HttpContext httpContext)
    {
        var result = _redisClient.SMembers(_cacheKeyManager.CommonQueueList).Select(x => new QueueResponse
            {
                QueueName = x,
                IsDelayQueue = false
            })
            .Concat(_redisClient.SMembers(_cacheKeyManager.DelayQueueList).Select(x => new QueueResponse
                {
                    QueueName = x,
                    IsDelayQueue = true
                })
            ).OrderBy(x => x.QueueName).ToList();

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(null, result));
    }
    #endregion

    #region ConsumerList
    private async Task ConsumerList(HttpContext httpContext)
    {
        var result = _redisClient.SMembers(_cacheKeyManager.ConsumerList).OrderBy(x => x).ToList();

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(null, result));
    }
    #endregion

    #region ServerList
    private async Task ServerList(HttpContext httpContext)
    {
        var result = _redisClient.HGetAll<DateTime>(_cacheKeyManager.ServerNodes).Select(x => new ServerResponse
        {
            ServerName = x.Key,
            HeartbeatTimestamp = TimeHelper.GetMillisecondTimestamp(x.Value)
        }).OrderByDescending(x => x.HeartbeatTimestamp).ToList();

        await httpContext.Response.WriteAsJsonAsync(BaseResponse.Success(null, result));
    }
    #endregion

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