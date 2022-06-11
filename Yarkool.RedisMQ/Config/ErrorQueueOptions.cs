using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
