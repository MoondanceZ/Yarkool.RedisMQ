using Yarkool.RedisMQ;

namespace RedisMQ.Console
{
    [RedisMQConsumer("Test")]
    internal class TestRedisMQConsumer : IRedisMQConsumer<TestMessage>
    {
        public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            System.Console.WriteLine(message.Input);

            return Task.CompletedTask;
        }
    }
}