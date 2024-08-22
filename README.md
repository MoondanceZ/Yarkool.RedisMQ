# Yarkool.RedisMQ

基于`Redis Stream`开发的队列服务, 包含发布者和消费者

## 用法

在 `Program.cs`中注册

```csharp
var cli = new RedisClient("127.0.0.1:6379,password=");
services.AddRedisMQ(cli, config =>
{
    config.UseErrorQueue = true;  //是否在消费错误时, 消息推送到错误队列
    config.RedisPrefix = "Test:";  //Redis缓存前缀
    config.RegisterConsumerService = false;  //是否开启队列消费服务
    config.RepublishNonAckTimeOutMessage = true;  //是否重新发布未正常Ack的消息到队列, 需要开启`RegisterConsumerService`
});
```

创建消费者, 需添加`RedisMQConsumer`特性, 设置`QueueName`, 消费者数量等, 延迟队列需要设置`IsDelayQueueConsumer = true`

```csharp
[RedisMQConsumer("Test")]
public class TestRedisMQConsumer : IRedisMQConsumer<TestMessage>
{
    public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine(message.Input);

        return Task.CompletedTask;
    }
}

[RedisMQConsumer("Delay", ConsumerCount = 1, PendingTimeOut = 10, IsDelayQueueConsumer = true)]
public class DelayConsumer(ILogger<DelayConsumer> logger) : IRedisMQConsumer<TestMessage>
{
    public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"message from delay queue: {message.Input}");
        return Task.CompletedTask;
    }
}
```

发布消息, 只需要注入`IRedisMQPublisher`, 调用`PublishAsync`, 参数`QueueName`需要跟消费者的`QueueName`一致

```csharp
private readonly IRedisMQPublisher _publisher;
public WeatherForecastController(IRedisMQPublisher publisher)
{
    _publisher = publisher;
}

// 发送普通队列消息
[HttpPost("PublishMessage")]
public async Task<string> PublishMessage()
{
    var input = Guid.NewGuid().ToString("N");
    var messageId = await _publisher.PublishMessageAsync("Test", new TestMessage
    {
        Input = input
    });
    return $"{messageId}-{input}";
}

// 发送延迟队列消息
[HttpPost("PublishDelayMessage")]
public async Task<string> PublishDelayMessage()
{
    var input = Guid.NewGuid().ToString("N");
    var messageId = await _publisher.PublishMessageAsync("Delay", new TestMessage
    {
        Input = input
    }, TimeSpan.FromSeconds(10));
    return $"{messageId}-{input}";
}
```

