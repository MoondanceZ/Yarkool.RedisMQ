using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public class QueueConfig
    {
        /// <summary>
        /// ErrorQueueOptions
        /// </summary>
        public ErrorQueueOptions? ErrorQueueOptions { get; private set; }

        /// <summary>
        /// MessageStorageTime, seconde
        /// </summary>
        public int MessageStorageTime { get; set; } = (int)TimeSpan.FromDays(7).TotalSeconds;

        /// <summary>
        /// RedisOption
        /// </summary>
        public RedisOptions RedisOptions { get; set; } = default!;

        /// <summary>
        /// UseConsumeErrorQueue
        /// </summary>
        /// <param name="options"></param>
        public void UseConsumeErrorQueue(Action<ErrorQueueOptions>? options = null)
        {
            if (options != null)
            {
                ErrorQueueOptions = new ErrorQueueOptions();
                options(ErrorQueueOptions);
            }
        }
    }
}
