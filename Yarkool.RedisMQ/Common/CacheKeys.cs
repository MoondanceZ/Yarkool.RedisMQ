namespace Yarkool.RedisMQ;

public class CacheKeys
{
    public const string MessageIdMapping = nameof(MessageIdMapping);

    public const string PublishSucceeded = nameof(PublishSucceeded);
    
    public const string TotalPublishSucceeded = nameof(TotalPublishSucceeded);

    public const string PublishFailed = nameof(PublishFailed);
    
    public const string TotalPublishFailed = nameof(TotalPublishFailed);

    public const string ConsumeSucceeded = nameof(ConsumeSucceeded);

    public const string TotalConsumeSucceeded = nameof(TotalConsumeSucceeded);

    public const string ConsumeFailed = nameof(ConsumeFailed);
    
    public const string TotalConsumeFailed = nameof(TotalConsumeFailed);

    public const string AckCount = nameof(AckCount);

    public const string TotalAckCount = nameof(TotalAckCount);
}