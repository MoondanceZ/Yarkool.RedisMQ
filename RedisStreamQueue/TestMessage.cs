using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.Redis.Queue;

namespace RedisStreamQueue
{
    [QueueAttribute("TestQueue")]
    public class TestMessage : BaseMessage
    {
        public string Input { get; set; }
    }
}
