using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10, PrefetchCount = 100, IsAutoAck = false)]
    public class TestConsumer : RedisMQConsumer<TestMessage>
    {
        public override async Task OnMessageAsync(TestMessage message, ConsumerReceivedDescriptor consumerReceivedDescriptor, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(message.Input);
            // throw new Exception("出错啦");
            await Task.Delay(10, cancellationToken);
            await consumerReceivedDescriptor.AckAsync();
        }

        public override Task OnErrorAsync(TestMessage message, Exception ex, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{DateTime.Now}: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}