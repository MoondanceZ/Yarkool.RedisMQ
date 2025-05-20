namespace Yarkool.RedisMQ;

public class CacheKeyManager(QueueConfig queueConfig)
{
    public string CommonQueueList => ParseCacheKey(nameof(CommonQueueList));

    public string DelayQueueList => ParseCacheKey(nameof(DelayQueueList));

    public string DelayQueueNameList => ParseCacheKey(nameof(DelayQueueNameList));

    public string ConsumerList => ParseCacheKey(nameof(ConsumerList));

    public string ServerNodes => ParseCacheKey(nameof(ServerNodes));

    public string PublishSucceeded => ParseCacheKey(nameof(PublishSucceeded));

    public string PublishFailed => ParseCacheKey(nameof(PublishFailed));

    public string ConsumeSucceeded => ParseCacheKey(nameof(ConsumeSucceeded));

    public string ConsumeFailed => ParseCacheKey(nameof(ConsumeFailed));

    public string AckCount => ParseCacheKey(nameof(AckCount));

    public string PublishMessageList => ParseCacheKey(nameof(PublishMessageList));
    
    public string PublishMessageIdSet => ParseCacheKey(nameof(PublishMessageIdSet));

    public string ParseCacheKey(string key)
    {
        if (string.IsNullOrWhiteSpace(queueConfig.RedisPrefix))
            return key;

        return $"{queueConfig.RedisPrefix}{key}";
    }

    public string GetStatusMessageIdSet(MessageStatus status)
    {
        return $"{status}MessageIdSet";
    }
}