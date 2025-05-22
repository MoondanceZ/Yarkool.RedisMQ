namespace Yarkool.RedisMQ;

public class ConsumerResponse
{
    /// <summary>
    /// ConsumerName
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;
    
    /// <summary>
    /// QueueName
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// ServerName
    /// </summary>
    public string ServerName { get; set; } = string.Empty;
}