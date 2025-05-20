namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public partial class QueueConfig
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
        /// 发布列表大小
        /// </summary>
        public int PublishListSize { get; set; } = 5000;

        /// <summary>
        /// 错误列表大小
        /// </summary>
        public int ErrorListSize { get; set; } = 20000;

        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerializer Serializer { get; set; } = new DefaultSerializer();

        /// <summary>
        /// UseDashboard
        /// </summary>
        /// <param name="options"></param>
        public void UseDashboard(Action<DashboardOptions>? options = null)
        {
            DashboardOptions = new DashboardOptions();
            options?.Invoke(DashboardOptions);
        }
    }
}