namespace Yarkool.RedisMQ;

public interface IMonitoringApi
{
    Task<BaseMessage?> GetPublishedMessageAsync(long id);

    Task<BaseMessage?> GetReceivedMessageAsync(long id);

    Task<StatisticsInfo> GetStatisticsAsync();

    Task<PageResponse<BaseMessage>> GetMessagesAsync(MessagePageRequest request);

    ValueTask<int> PublishedFailedCount();

    ValueTask<int> PublishedSucceededCount();

    ValueTask<int> ReceivedFailedCount();

    ValueTask<int> ReceivedSucceededCount();

    Task<IDictionary<DateTime, int>> HourlySucceededJobs(MessageType type);

    Task<IDictionary<DateTime, int>> HourlyFailedJobs(MessageType type);
}