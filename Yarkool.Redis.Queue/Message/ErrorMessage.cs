using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    public class ErrorMessage<TMessage> where TMessage : BaseMessage
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp
        {
            get
            {
                return (DateTime.Now.ToUniversalTime().Ticks - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks) / 10000;
            }
        }

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
        public string ConsumerName { get; set; } = default!;

        /// <summary>
        /// 消息
        /// </summary>
        public TMessage Message { get; set; } = default!;
    }
}
