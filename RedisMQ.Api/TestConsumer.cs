using Yarkool.RedisMQ;

namespace RedisMQ.Api
{
    [RedisMQConsumer("Test", ConsumerCount = 1, PendingTimeOut = 10)]
    public class TestConsumer(ILogger<TestConsumer> logger) : IRedisMQConsumer<TestMessage>
    {
        public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(message.Input);
            return Task.CompletedTask;
        }
    }
}