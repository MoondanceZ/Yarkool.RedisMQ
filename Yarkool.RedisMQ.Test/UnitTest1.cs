using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using RedisMQ.Api.Test;

namespace Yarkool.RedisMQ.Test;

public class Tests
{
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
        services.AddRedisMQ(cli, config =>
        {
            config.RedisPrefix = "RedisMQ:";
            config.RegisterConsumerService = false;
            config.RepublishNonAckTimeOutMessage = true;
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task Test1()
    {
        var publisher = _serviceProvider.GetService<IRedisMQPublisher>();
        for (int i = 0; i < 10000; i++)
        {
            await publisher.PublishMessageAsync("Test", new TestMessage { Input = i.ToString() });
        }

        Assert.Pass();
    }
}