using RedisMQ.Api.Messages;
using Yarkool.RedisMQ;

namespace RedisMQ.Api.Consumers;

[RedisMQConsumer("Delay", ConsumerCount = 1, PendingTimeOut = 200, IsDelayQueueConsumer = true)]
public class DelayConsumer(ILogger<DelayConsumer> logger) : RedisMQConsumer<TestMessage>
{
    public override Task OnMessageAsync(TestMessage message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"message from delay queue: {message.Input}");
        return Task.CompletedTask;
    }
}