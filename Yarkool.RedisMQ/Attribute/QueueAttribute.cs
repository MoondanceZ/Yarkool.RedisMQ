using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.RedisMQ
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class QueueAttribute : System.Attribute
    {

        public QueueAttribute(string queueName)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; private set; } = default!;

        /// <summary>
        /// 消费者数量
        /// </summary>
        public int SubscriberCount { get; set; } = 1;

        /// <summary>
        /// 等待超时时间, 默认: 5分钟
        /// </summary>
        public int PenddingTimeOut { get; set; } = 5 * 60;
    }
}
