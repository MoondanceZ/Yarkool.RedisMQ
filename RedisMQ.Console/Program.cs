using FreeRedis;
using Microsoft.Extensions.Hosting;
using Yarkool.RedisMQ;

Console.WriteLine("Consumer init");

var build = new HostBuilder()
    .ConfigureServices(services =>
    {
        var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
        //cli.Notice += (s, e) => Console.WriteLine(e.Log);

        services.AddRedisMQ(cli, config =>
        {
            config.RedisPrefix = "Test:";
            config.RegisterConsumerService = false;
            config.RepublishNonAckTimeOutMessage = true;
            config.UseErrorQueue();
        });
    });

await build.RunConsoleAsync();