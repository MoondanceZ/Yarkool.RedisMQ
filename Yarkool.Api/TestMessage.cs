using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    [QueueAttribute("TestQueue", SubscriberCount = 2)]
    public class TestMessage : BaseMessage
    {
        public string? Input { get; set; }
    }
}
