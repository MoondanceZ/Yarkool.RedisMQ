namespace Yarkool.RedisMQ;

internal class ServerNode
{
    /// <summary>
    /// ServerName
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// LiveTime
    /// </summary>
    public DateTime LiveTime { get; set; }

    /// <summary>
    /// ConsumerList
    /// </summary>
    public IEnumerable<string> ConsumerList { get; set; } = [];
}