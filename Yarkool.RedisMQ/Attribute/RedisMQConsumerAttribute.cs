namespace Yarkool.RedisMQ
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RedisMQConsumerAttribute(string queueName) : Attribute
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; private set; } = queueName;

        /// <summary>
        /// 是否是延迟队列消费者
        /// </summary>
        public bool IsDelayQueueConsumer { get; set; }

        /// <summary>
        /// 消费者数量
        /// </summary>
        public int ConsumerCount { get; set; } = 1;

        /// <summary>
        /// 等待超时时间, 单位: 秒, 默认: 300秒
        /// </summary>
        public int PendingTimeOut { get; set; } = 5 * 60;

        /// <summary>
        /// 拉取消息数量
        /// </summary>
        public int PrefetchCount { get; set; } = 10;
    }
}