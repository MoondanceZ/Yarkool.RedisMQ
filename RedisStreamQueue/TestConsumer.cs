using FreeRedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.Redis.Queue;

namespace RedisStreamQueue
{
    internal class TestConsumer : AbstractConsumer<TestMessage>
    {
        public TestConsumer(QueueConfig queueConfig, RedisClient redisClient) : base(queueConfig, redisClient)
        {
        }

        public override Action OnError()
        {
            throw new NotImplementedException();
        }

        public override Action OnMessage()
        {
            throw new NotImplementedException();
        }
    }
}
