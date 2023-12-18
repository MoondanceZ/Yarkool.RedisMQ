using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    [QueuePublisher("Test")]
    public class TestPublisher : BasePublisher<TestMessage>
    {
    }
}