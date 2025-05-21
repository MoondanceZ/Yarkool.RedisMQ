using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 30, PrefetchCount = 100, IsAutoAck = false, AutomaticRetryAttempts = 1)]
    public class TestConsumer : RedisMQConsumer<TestMessage>
    {
        public override async Task OnMessageAsync(TestMessage message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(message.Input);
            // throw new Exception("出错啦");
            await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);

            // IsAutoAck = false, manual ack
            await messageHandler.AckAsync(cancellationToken);
        }

        public override Task OnErrorAsync(TestMessage message, ConsumerMessageHandler messageHandler, Exception ex, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{DateTime.Now}: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}