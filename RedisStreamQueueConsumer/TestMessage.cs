using Yarkool.RedisMQ;

namespace RedisStreamQueue
{
    internal class TestMessage : BaseMessage
    {
        public string? Input { get; set; }
    }
}