using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue.Message
{
    public class BaseMessage
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
    }
}
