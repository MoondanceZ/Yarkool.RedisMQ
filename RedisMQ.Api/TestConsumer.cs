using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10, PrefetchCount = 100)]
    public class TestConsumer : IRedisMQConsumer<TestMessage>
    {
        public async Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(message.Input);
            await Task.Delay(10, cancellationToken);
        }
    }
}