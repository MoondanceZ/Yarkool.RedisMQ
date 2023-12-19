using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 2, PendingTimeOut = 2)]
    public class TestConsumer(ILogger<TestConsumer> logger) : IRedisMQConsumer<TestMessage>
    {
        public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(message.Input);
            // Console.WriteLine(message.Input);
            return Task.CompletedTask;
        }
    }
}