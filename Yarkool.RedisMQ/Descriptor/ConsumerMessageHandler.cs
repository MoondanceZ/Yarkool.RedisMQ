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
        if (!messageIds.Any())
            return;
        var streamMessageIdDic = new Dictionary<string, string>();
        foreach (var id in messageIds)
        {
            if (string.IsNullOrEmpty(id))
                continue;
            var streamMessageId = await redisClient.HGetAsync(cacheKeyManager.MessageIdMapping, id).ConfigureAwait(false);
            streamMessageIdDic.Add(id, streamMessageId);
        }

        if (streamMessageIdDic.Any())
        {
            var time = DateTime.Now.ToString("yyyyMMddHH");
            using var tran = redisClient.Multi();
            foreach (var item in streamMessageIdDic)
            {
                tran.XAck(queueName, groupName, item.Value);
                tran.XDel(queueName, item.Value);
                tran.HDel(cacheKeyManager.MessageIdMapping, item.Key);
                tran.IncrBy($"{cacheKeyManager.AckCount}:Total", 1);
                tran.IncrBy($"{cacheKeyManager.AckCount}:{time}", 1);
                tran.Expire($"{cacheKeyManager.AckCount}:{time}", TimeSpan.FromHours(30));
            }

            tran.Exec();
        }
    }
}