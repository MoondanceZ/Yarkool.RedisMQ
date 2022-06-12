using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.RedisMQ
{
    public class BaseMessage
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; } = TimeHelper.GetMillisecondTimestamp();
    }
}
