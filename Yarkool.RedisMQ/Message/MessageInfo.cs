namespace Yarkool.RedisMQ;

public class MessageInfo
{
    /// <summary>
    /// MessageId
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// MessageStatus
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    /// <summary>
    /// Content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// CreateTimestamp
    /// </summary>
    public long CreateTimestamp { get; set; } = 0;
}