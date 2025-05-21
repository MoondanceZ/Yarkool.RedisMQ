namespace Yarkool.RedisMQ;

public class QueueResponse
{
    /// <summary>
    /// QueueName
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// IsDelayQueue
    /// </summary>
    public bool IsDelayQueue { get; set; }
}