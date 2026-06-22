using FreeRedis;

namespace Yarkool.RedisMQ;

public class ConsumerMessageHandler
(
    string queueName,
    string groupName,
    IRedisClient redisClient,
    CacheKeyManager cacheKeyManager
)
{
    /// <summary>
    /// 消息Id
    /// </summary>
    internal string? MessageId { get; set; }

    /// <summary>
    /// Redis Stream message id
    /// </summary>
    internal string? StreamMessageId { get; set; }

    /// <summary>
    /// 是否已确认当前消息
    /// </summary>
    internal bool IsAcknowledged { get; private set; }

    /// <summary>
    /// Ack
    /// </summary>
    /// <returns></returns>
    public async Task AckAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(MessageId))
        {
            await AckAsync([MessageId], cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ack multiple message
    /// </summary>
    /// <returns></returns>
    public async Task AckAsync(IEnumerable<string> messageIds, CancellationToken cancellationToken = default)
    {
        var messageIdList = messageIds
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToArray();

        if (messageIdList.Length == 0)
            return;

        var streamMessageIdDic = new Dictionary<string, string>();
        foreach (var id in messageIdList)
        {
            var streamMessageId = id == MessageId && !string.IsNullOrEmpty(StreamMessageId)
                ? StreamMessageId
                : await redisClient.HGetAsync($"{cacheKeyManager.PublishMessageList}:{id}", "Id").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(streamMessageId))
                streamMessageIdDic[id] = streamMessageId;
        }

        if (streamMessageIdDic.Any())
        {
            var time = DateTime.Now.ToString("yyyyMMddHH");
            var now = TimeHelper.GetMillisecondTimestamp();
            using var tran = redisClient.Multi();
            foreach (var item in streamMessageIdDic)
            {
                tran.XAck(queueName, groupName, item.Value);
                tran.XDel(queueName, item.Value);
                tran.IncrBy($"{cacheKeyManager.AckCount}:Total", 1);
                tran.IncrBy($"{cacheKeyManager.AckCount}:{time}", 1);
                tran.Expire($"{cacheKeyManager.AckCount}:{time}", TimeSpan.FromHours(30));
                tran.HSet($"{cacheKeyManager.PublishMessageList}:{item.Key}", "Status", MessageStatus.Completed);
                tran.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), now, item.Key);
                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), item.Key);
                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), item.Key);
                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), item.Key);
                tran.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), item.Key);
            }

            tran.Exec();

            if (!string.IsNullOrEmpty(MessageId) && streamMessageIdDic.ContainsKey(MessageId))
                IsAcknowledged = true;
        }
    }
}
