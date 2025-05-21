namespace Yarkool.RedisMQ;

public class QueueResponse
{
    /// <summary>
    /// QueueName
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// IsDelayQueue
    /// </summary>
    public bool IsDelayQueue { get; set; }

    /// <summary>
    /// QueueStatus
    /// </summary>
    public QueueStatus Status { get; set; }
}