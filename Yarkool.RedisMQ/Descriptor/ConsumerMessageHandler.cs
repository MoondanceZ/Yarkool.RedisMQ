using FreeRedis;

namespace Yarkool.RedisMQ;

public class ConsumerMessageHandler
(
    string queueName,
    string groupName,
    IRedisClient redisClient
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
            var streamMessageId = await redisClient.HGetAsync(CacheKeys.MessageIdMapping, MessageId).ConfigureAwait(false);

            using var tran = redisClient!.Multi();
            tran.XAck(queueName, groupName, streamMessageId);
            tran.XDel(queueName, streamMessageId);
            tran.HDel(CacheKeys.MessageIdMapping, MessageId);
            tran.Exec();
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
            var streamMessageId = await redisClient.HGetAsync(CacheKeys.MessageIdMapping, id).ConfigureAwait(false);
            streamMessageIdDic.Add(id, streamMessageId);
        }

        if (streamMessageIdDic.Any())
        {
            using var tran = redisClient!.Multi();
            foreach (var item in streamMessageIdDic)
            {
                tran.XAck(queueName, groupName, item.Value);
                tran.XDel(queueName, item.Value);
                tran.HDel(CacheKeys.MessageIdMapping, item.Key);
            }

            tran.Exec();
        }
    }
}