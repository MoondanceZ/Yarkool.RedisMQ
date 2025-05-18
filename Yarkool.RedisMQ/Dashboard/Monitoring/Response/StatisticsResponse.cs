namespace Yarkool.RedisMQ;

internal class StatisticsResponse
{
    /// <summary>
    /// PublishSucceeded
    /// </summary>
    public long PublishSucceeded { get; set; }

    /// <summary>
    /// PublishFailed
    /// </summary>
    public long PublishFailed { get; set; }

    /// <summary>
    /// ConsumeSucceeded
    /// </summary>
    public long ConsumeSucceeded { get; set; }

    /// <summary>
    /// ConsumeFailed
    /// </summary>
    public long ConsumeFailed { get; set; }

    /// <summary>
    /// AckCount
    /// </summary>
    public long AckCount { get; set; }

    /// <summary>
    /// ErrorQueueLength
    /// </summary>
    public long ErrorQueueLength { get; set; }
}