using Yarkool.RedisMQ;

namespace RedisMQ.Console
{
    [RedisMQConsumer("Test")]
    internal class TestRedisMQConsumer : RedisMQConsumer<TestMessage>
    {
        public override Task OnMessageAsync(TestMessage message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken = default)
        {
            System.Console.WriteLine(message.Input);

            return Task.CompletedTask;
        }
    }
}