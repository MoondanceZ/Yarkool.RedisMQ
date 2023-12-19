namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public class QueueConfig
    {
        /// <summary>
        /// MessageStorageTime, seconde
        /// </summary>
        public int MessageStorageTime { get; set; } = (int) TimeSpan.FromDays(7).TotalSeconds;

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
        /// </summary>
        public bool UseErrorQueue { get; set; } = true;

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