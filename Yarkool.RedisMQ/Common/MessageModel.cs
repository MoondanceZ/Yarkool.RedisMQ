namespace Yarkool.RedisMQ;

internal class MessageModel
{
    /// <summary>
    /// queue name
    /// </summary>
    public string QueueName { get; set; } = default!;

    /// <summary>
    /// delay queue name
    /// </summary>
    public string? DelayQueueName { get; set; }

    /// <summary>
    /// stream queue message id
    /// </summary>
    public string? QueueMessageId { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// message
    /// </summary>
    public BaseMessage Message { get; set; } = default!;
}