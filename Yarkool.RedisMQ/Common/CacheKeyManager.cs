namespace Yarkool.RedisMQ;

public class CacheKeyManager(QueueConfig queueConfig)
{
    public string CommonQueueList => ParseCacheKey($"Server:{nameof(CommonQueueList)}");

    public string DelayQueueList => ParseCacheKey($"Server:{nameof(DelayQueueList)}");

    public string DelayQueueNameList => ParseCacheKey($"Server:{nameof(DelayQueueNameList)}");

    public string ConsumerList => ParseCacheKey($"Server:{nameof(ConsumerList)}");

    public string ServerNodes => ParseCacheKey($"Server:{nameof(ServerNodes)}");

    public string PublishSucceeded => ParseCacheKey($"Stat:{nameof(PublishSucceeded)}");

    public string PublishFailed => ParseCacheKey($"Stat:{nameof(PublishFailed)}");

    public string ConsumeSucceeded => ParseCacheKey($"Stat:{nameof(ConsumeSucceeded)}");

    public string ConsumeFailed => ParseCacheKey($"Stat:{nameof(ConsumeFailed)}");

    public string AckCount => ParseCacheKey($"Stat:{nameof(AckCount)}");

    public string PublishMessageList => ParseCacheKey($"Message:PublishList");

    public string PublishMessageIdSet => ParseCacheKey("Message:IdSet:Publish");

    public string ParseCacheKey(string key)
    {
        if (string.IsNullOrWhiteSpace(queueConfig.RedisPrefix))
            return key;

        return $"{queueConfig.RedisPrefix}{key}";
    }

    public string GetStatusMessageIdSet(MessageStatus status)
    {
        return ParseCacheKey($"Message:IdSet:{status}");
    }

    public string GetQueueName(string queueName)
    {
        return ParseCacheKey($"Queue:{queueName}");
    }
}