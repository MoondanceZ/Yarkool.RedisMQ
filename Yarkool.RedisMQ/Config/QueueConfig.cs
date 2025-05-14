namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public class QueueConfig
    {
        /// <summary>
        /// 注册消费者服务
        /// </summary>
        public bool RegisterConsumerService { get; set; }

        /// <summary>
        /// 自动重新发布未Ack超时的消息
        /// </summary>
        public bool RepublishNonAckTimeOutMessage { get; set; } = true;

        /// <summary>
        /// 使用失败队列
        /// <para>消费失败时, 消息会流转到失败队列, 原队列的消息将会被删除</para>
        /// </summary>
        public bool UseErrorQueue { get; set; }

        /// <summary>
        /// Redis缓存前缀
        /// </summary>
        public string? RedisPrefix { get; set; }

        /// <summary>
        /// 消息消费失败队列
        /// </summary>
        public string ErrorQueueName { get; set; } = "ErrorQueue";

        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerializer Serializer { get; set; } = new DefaultSerializer();

        ///// <summary>
        ///// UseConsumeErrorQueue
        ///// </summary>
        ///// <param name="options"></param>
        //public void UseConsumeErrorQueue(Action<ErrorQueueOptions>? options = null)
        //{
        //    if (options != null)
        //    {
        //        ErrorQueueOptions = new ErrorQueueOptions();
        //        options(ErrorQueueOptions);
        //    }
        //}
    }
}