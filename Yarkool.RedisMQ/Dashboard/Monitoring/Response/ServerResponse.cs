namespace Yarkool.RedisMQ;

public class ServerResponse
{
    /// <summary>
    /// ServerName
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// HeartbeatTimestamp
    /// </summary>
    public long HeartbeatTimestamp { get; set; }
}