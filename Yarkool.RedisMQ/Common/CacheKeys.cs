namespace Yarkool.RedisMQ;

public class CacheKeys
{
    public const string MessageIdMapping = nameof(MessageIdMapping);

    public const string PublishSucceeded = nameof(PublishSucceeded);

    public const string PublishFailed = nameof(PublishFailed);

    public const string ConsumeSucceeded = nameof(ConsumeSucceeded);

    public const string ConsumeFailed = nameof(ConsumeFailed);
    
    public const string AckCount = nameof(AckCount);

}