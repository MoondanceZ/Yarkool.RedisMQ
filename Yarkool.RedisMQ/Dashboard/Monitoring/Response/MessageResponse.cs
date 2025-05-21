namespace Yarkool.RedisMQ;

internal class MessageResponse
{
    /// <summary>
    /// MessageStatus
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// ExecutionTimes
    /// </summary>
    public int ExecutionTimes { get; set; }

    /// <summary>
    /// ErrorInfo
    /// </summary>
    public MessageErrorInfo? ErrorInfo { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public BaseMessage Message { get; set; } = default!;
}