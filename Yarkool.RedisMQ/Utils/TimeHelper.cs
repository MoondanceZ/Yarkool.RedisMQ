using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.RedisMQ
{
    internal static class TimeHelper
    {
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long GetMillisecondTimestamp(DateTime? dateTime = null)
        {
            return ((dateTime ?? DateTime.Now).ToUniversalTime().Ticks - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks) / 10000;
        }
    }
}
