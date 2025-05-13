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
    /// 是否是延迟队列消费者
    /// </summary>
    public bool IsDelayQueueConsumer { get; set; }

    /// <summary>
    /// 等待超时时间, 单位: 秒, 默认: 300秒
    /// </summary>
    public int PendingTimeOut { get; set; } = 300;

    /// <summary>
    /// 拉取消息数量
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// 是否自动 Ack
    /// </summary>
    public bool IsAutoAck { get; set; } = true;

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