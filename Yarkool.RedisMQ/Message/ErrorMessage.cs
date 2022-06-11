using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.RedisMQ
{
    /// <summary>
    /// 错误队列
    /// </summary>
    [Queue("Error")]
    public class ErrorMessage : BaseMessage
    {
        /// <summary>
        /// 对列名称
        /// </summary>
        public string QueueName { get; set; } = default!;

        /// <summary>
        /// 队列组名
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 消费者名称
        /// </summary>
        public string SubscriberName { get; set; } = default!;

        /// <summary>
        /// 错误内容
        /// </summary>
        public string ExceptionMessage { get; set; } = default!;

        /// <summary>
        /// 堆栈信息
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string MessageContent { get; set; } = default!;
    }
}
