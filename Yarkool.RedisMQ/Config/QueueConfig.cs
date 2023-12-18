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
        /// 自动初始化订阅者
        /// </summary>
        public bool AutoInitSubscriber { get; set; }

        /// <summary>
        /// 自动初始化发布者
        /// </summary>
        public bool AutoInitPublisher { get; set; }

        /// <summary>
        /// 自动重新发布超时消息
        /// </summary>
        public bool AutoRePublishTimeOutMessage { get; set; }

        /// <summary>
        /// 使用失败队列
        /// </summary>
        public bool UseErrorQueue { get; set; }

        /// <summary>
        /// Redis缓存前缀
        /// </summary>
        public string? RedisPrefix { get; set; }

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