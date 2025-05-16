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
        /// Redis缓存前缀
        /// </summary>
        public string? RedisPrefix { get; set; }

        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerializer Serializer { get; set; } = new DefaultSerializer();

        /// <summary>
        /// DashboardOptions
        /// </summary>
        internal DashboardOptions? DashboardOptions { get; set; }

        /// <summary>
        /// ErrorQueueOptions
        /// </summary>
        internal ErrorQueueOptions? ErrorQueueOptions { get; set; }

        /// <summary>
        /// 使用失败队列
        /// </summary>
        /// <param name="options"></param>
        public void UseErrorQueue(Action<ErrorQueueOptions>? options = null)
        {
            ErrorQueueOptions = new ErrorQueueOptions();
            if (options != null)
            {
                options.Invoke(ErrorQueueOptions);
            }
        }
    }
}