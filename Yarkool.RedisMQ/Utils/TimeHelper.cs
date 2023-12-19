namespace Yarkool.RedisMQ
{
    internal static class TimeHelper
    {
        /// <summary>
        /// 获取当前时间戳(秒)
        /// </summary>
        /// <returns></returns>
        public static long GetSecondTimestamp(DateTime? dateTime = null)
        {
            return new DateTimeOffset(dateTime.GetValueOrDefault(DateTime.Now)).ToUnixTimeSeconds();
        }

        /// <summary>  
        /// 获取当前时间戳(毫秒)
        /// </summary>  
        /// <returns>long</returns>  
        public static long GetMillisecondTimestamp(DateTime? dateTime = null)
        {
            return new DateTimeOffset(dateTime.GetValueOrDefault(DateTime.Now)).ToUnixTimeMilliseconds();
        }
    }
}