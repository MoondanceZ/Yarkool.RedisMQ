﻿using Yarkool.RedisMQ;

namespace RedisMQ.Api;

[RedisMQConsumer("Delay", ConsumerCount = 1, PendingTimeOut = 10, IsDelayQueueConsumer = true)]
public class DelayConsumer(ILogger<DelayConsumer> logger) : RedisMQConsumer<TestMessage>
{
    public override Task OnMessageAsync(TestMessage message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"message from delay queue: {message.Input}");
        return Task.CompletedTask;
    }
}