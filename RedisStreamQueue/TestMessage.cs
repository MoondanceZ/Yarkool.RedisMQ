using Yarkool.RedisMQ;

namespace RedisStreamQueue
{
    public class TestMessage : BaseMessage
    {
        public string? Input { get; set; }
    }
}