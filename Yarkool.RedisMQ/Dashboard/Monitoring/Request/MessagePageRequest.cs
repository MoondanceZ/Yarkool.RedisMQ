namespace Yarkool.RedisMQ;

internal class MessagePageRequest : PageRequest
{
    /// <summary>
    /// Status
    /// </summary>
    public MessageStatus? Status { get; set; }
}