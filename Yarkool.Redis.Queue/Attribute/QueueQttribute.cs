using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class QueueQttribute: System.Attribute
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; } = default!;

        /// <summary>
        /// 消费者数量
        /// </summary>
        public int ConsumerCount { get; set; } = 1;

        /// <summary>
        /// 等待超时时间, 默认: 5分钟
        /// </summary>
        public int PenddingTimeOut { get; set; } = 5 * 60;
    }
}
