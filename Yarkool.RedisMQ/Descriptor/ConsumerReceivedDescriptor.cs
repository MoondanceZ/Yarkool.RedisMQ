using FreeRedis;

namespace Yarkool.RedisMQ;

public class ConsumerReceivedDescriptor(string queueName, string groupName, string messageId, IRedisClient redisClient)
{
    /// <summary>
    /// Ack
    /// </summary>
    /// <returns></returns>
    public async Task AckAsync()
    {
        var streamMessageId = await redisClient.HGetAsync(Constants.MessageIdMapping, messageId).ConfigureAwait(false);
        
        using var tran = redisClient!.Multi();
        tran.XAck(queueName, groupName, streamMessageId);
        tran.XDel(queueName, streamMessageId);
        tran.HDel(Constants.MessageIdMapping, messageId);
        tran.Exec();
    }
}