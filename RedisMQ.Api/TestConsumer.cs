using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10, PrefetchCount = 100)]
    public class TestConsumer : IRedisMQConsumer<TestMessage>
    {
        public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(message.Input);
            return Task.CompletedTask;
        }
    }
}