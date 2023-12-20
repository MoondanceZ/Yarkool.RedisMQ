using Yarkool.RedisMQ;

namespace RedisMQ.Api;

[RedisMQConsumer("Delay", ConsumerCount = 1, PendingTimeOut = 10, IsDelayQueueConsumer = true)]
public class DelayConsumer(ILogger<DelayConsumer> logger) : IRedisMQConsumer<TestMessage>
{
    public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"message from delay queue: {message.Input}");
        return Task.CompletedTask;
    }
}