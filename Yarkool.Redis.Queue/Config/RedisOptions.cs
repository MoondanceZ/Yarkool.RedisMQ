using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    /// <summary>
    /// Reids Options
    /// </summary>
    public class RedisOptions
    {
        ///// <summary>
        ///// Host
        ///// </summary>
        //public string Host { get; set; } = default!;

        ///// <summary>
        ///// Password
        ///// </summary>
        //public string Password { get; set; } = default!;

        /// <summary>
        /// Prefix
        /// </summary>
        public string? Prefix { get; set; }

        ///// <summary>
        ///// Database
        ///// </summary>
        //public int Database { get; set; } = 1;
    }
}
