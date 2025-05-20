namespace Yarkool.RedisMQ;

internal class MessagePageRequest : PageRequest
{
    /// <summary>
    /// Status
    /// </summary>
    public int? Status { get; set; }
}