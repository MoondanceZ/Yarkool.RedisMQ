# Yarkool.RedisMQ

基于`Redis Stream`开发的队列服务, 包含发布者和消费者

##  用法
在 `Program.cs`中注册
```csharp
services.AddRedisMQ(cli, config =>
{
    config.UseErrorQueue = true;  //是否在消费错误时, 消息推送到错误队列
    config.RedisPrefix = "Test:";  //Redis缓存前缀
    config.RegisterConsumerService = false;  //是否开启队列消费服务
    config.RepublishNonAckTimeOutMessage = true;  //是否重新发布未正常Ack的消息到队列, 需要开启`RegisterConsumerService`
});
```

创建消费者, 需添加`RedisMQConsumer`特性, 设置`QueueName`, 消费者数量等
```csharp
[RedisMQConsumer("Test")]
internal class TestRedisMqConsumer : IRedisMQConsumer<TestMessage>
{
    public Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine(message.Input);

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

[HttpGet("GenMessage")]
public async Task<string> GenMessage()
{
    var input = Guid.NewGuid().ToString("N");
    var messageId = await _publisher.PublishAsync("Test", new TestMessage
    {
        Input = input
    });
    return $"{messageId}-{input}";
}
```

