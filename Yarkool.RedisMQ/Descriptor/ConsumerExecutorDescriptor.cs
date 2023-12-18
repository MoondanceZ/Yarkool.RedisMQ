using System.Reflection;

namespace Yarkool.RedisMQ;

public class ConsumerExecutorDescriptor
{
    /// <summary>
    /// QueueName
    /// </summary>
    public string QueueName { get; set; } = default!;

    /// <summary>
    /// GroupName
    /// </summary>
    public string GroupName { get; set; } = default!;

    /// <summary>
    /// QueueConsumerAttribute
    /// </summary>
    public QueueConsumerAttribute QueueConsumerAttribute { get; set; } = default!;

    /// <summary>
    /// QueueTypeInfo
    /// </summary>
    public TypeInfo TypeInfo { get; set; } = default!;
}