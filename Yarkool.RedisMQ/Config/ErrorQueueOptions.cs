namespace Yarkool.RedisMQ
{
    /// <summary>
    /// ErrorQueueOption
    /// </summary>
    public class ErrorQueueOptions
    {
        /// <summary>
        /// QueueName
        /// </summary>
        public string QueueName { get; set; } = "ErrorQueue";

        /// <summary>
        /// 推送到失败队列后, 是否删除原始队列的消息
        /// </summary>
        public bool IsDeleteOriginalQueueMessage { get; set; } = true;
    }
}