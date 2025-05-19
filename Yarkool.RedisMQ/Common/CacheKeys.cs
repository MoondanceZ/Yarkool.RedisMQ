namespace Yarkool.RedisMQ;

public class CacheKeys
{
    public const string QueueList = nameof(QueueList);
    
    public const string ConsumerList = nameof(ConsumerList);
    
    public const string ServerNodes = nameof(ServerNodes);
    
    public const string MessageIdMapping = nameof(MessageIdMapping);

    public const string PublishSucceeded = nameof(PublishSucceeded);

    public const string PublishFailed = nameof(PublishFailed);

    public const string ConsumeSucceeded = nameof(ConsumeSucceeded);

    public const string ConsumeFailed = nameof(ConsumeFailed);
    
    public const string AckCount = nameof(AckCount);

}