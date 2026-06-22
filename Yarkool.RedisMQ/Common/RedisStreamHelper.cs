using FreeRedis;

namespace Yarkool.RedisMQ;

internal static class RedisStreamHelper
{
    private const string ConsumerGroupStartId = "0-0";

    public static void EnsureConsumerGroups(IRedisClient redisClient, CacheKeyManager cacheKeyManager, IEnumerable<ConsumerExecutorDescriptor> consumerExecutorDescriptors)
    {
        foreach (var descriptor in consumerExecutorDescriptors)
        {
            var queueNameKey = cacheKeyManager.GetQueueName(descriptor.QueueName);
            EnsureConsumerGroup(redisClient, queueNameKey, descriptor.GroupName);
        }
    }

    public static void EnsureConsumerGroup(IRedisClient redisClient, string queueNameKey, string groupName)
    {
        try
        {
            redisClient.XGroupCreate(queueNameKey, groupName, ConsumerGroupStartId, MkStream: true);
        }
        catch (Exception ex) when (IsBusyGroupException(ex))
        {
        }
    }

    private static bool IsBusyGroupException(Exception ex)
    {
        return ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("Consumer Group name already exists", StringComparison.OrdinalIgnoreCase);
    }
}
