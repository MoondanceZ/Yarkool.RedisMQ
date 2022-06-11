﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.RedisMQ;

namespace RedisStreamQueue
{
    [QueueAttribute("TestQueue", SubscriberCount = 2)]
    internal class TestMessage : BaseMessage
    {
        public string? Input { get; set; }
    }
}
