using FreeRedis;
using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10, PrefetchCount = 100)]
    public class TestConsumer(IRedisClient redisClient) : IRedisMQConsumer<TestMessage>
    {
        public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            redisClient.LPush("Test:List", message.Input);
            return Task.CompletedTask;
        }
    }
}