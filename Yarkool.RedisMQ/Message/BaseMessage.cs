namespace Yarkool.RedisMQ
{
    public class BaseMessage
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public long CreateTimestamp { get; set; } = TimeHelper.GetMillisecondTimestamp();

        /// <summary>
        /// 消息内容
        /// </summary>
        public object MessageContent { get; set; } = default!;

        /// <summary>
        /// 机器名称
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;
    }
}