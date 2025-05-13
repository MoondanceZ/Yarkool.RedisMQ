using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10, PrefetchCount = 100)]
    public class TestConsumer : IRedisMQConsumer<TestMessage>
    {
        public async Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(message.Input);
            // throw new Exception("出错啦");
            await Task.Delay(10, cancellationToken);
        }

        public Task OnErrorAsync(TestMessage message, Exception ex, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{DateTime.Now}: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}