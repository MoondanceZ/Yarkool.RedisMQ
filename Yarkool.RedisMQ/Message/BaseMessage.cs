namespace Yarkool.RedisMQ
{
    internal class BaseMessage
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public long CreateTimestamp { get; set; } = TimeHelper.GetMillisecondTimestamp();

        /// <summary>
        /// 消息内容
        /// </summary>
        public string? MessageContent { get; set; }

        /// <summary>
        /// 机器名称
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// 延迟时间, 单位: 秒
        /// </summary>
        public double DelayTime { get; set; }

        /// <summary>
        /// 消息Id
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    }
}