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
    }
}
