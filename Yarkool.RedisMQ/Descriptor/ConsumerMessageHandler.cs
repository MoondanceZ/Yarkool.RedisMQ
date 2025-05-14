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
            var streamMessageId = await redisClient.HGetAsync(Constants.MessageIdMapping, MessageId).ConfigureAwait(false);

            using var tran = redisClient!.Multi();
            tran.XAck(queueName, groupName, streamMessageId);
            tran.XDel(queueName, streamMessageId);
            tran.HDel(Constants.MessageIdMapping, MessageId);
            tran.Exec();
        }
    }
}