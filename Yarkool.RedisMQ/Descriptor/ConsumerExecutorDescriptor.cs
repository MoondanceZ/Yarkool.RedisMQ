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
    /// RedisMQConsumerAttribute
    /// </summary>
    public RedisMQConsumerAttribute RedisMQConsumerAttribute { get; set; } = default!;

    /// <summary>
    /// TypeInfo
    /// </summary>
    public TypeInfo ConsumerTypeInfo { get; set; } = default!;

    /// <summary>
    /// MessageTypeInfo
    /// </summary>
    public TypeInfo MessageTypeInfo { get; set; } = default!;
}