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
        /// 消费者数量
        /// </summary>
        public int ConsumerCount { get; set; } = 1;

        /// <summary>
        /// 等待超时时间, 默认: 5分钟
        /// </summary>
        public int PendingTimeOut { get; set; } = 5 * 60;
    }
}