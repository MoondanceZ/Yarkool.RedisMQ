// See https://aka.ms/new-console-template for more information

using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RedisStreamQueue;
using Yarkool.RedisMQ;

var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
cli.Notice += (s, e) => Console.WriteLine(e.Log);

var services = new ServiceCollection();
services.AddRedisMQ(cli, config =>
{
    config.UseErrorQueue = true;
    config.RedisPrefix = "Test:";
});

//services.AddTransient<TestPublisher>();

using var buildServiceProvider = services.BuildServiceProvider();
var serviceProvider = buildServiceProvider.CreateScope().ServiceProvider;
var testPublisher = serviceProvider.GetService<TestPublisher>()!;


if (!cli.Exists("x-stream"))
{
    cli.XGroupCreate("x-stream", "group1", MkStream: true);
    cli.XGroupCreate("x-stream", "group2", MkStream: true);
}
else
{
    //多个分组表示消息会分发到多个分组, 消息也会被消费多次
    var infoGroups = cli.XInfoGroups("x-stream");
    if (!infoGroups.Any(x => x.name == "group1"))
        cli.XGroupCreate("x-stream", "group1", MkStream: true);

    if (!infoGroups.Any(x => x.name == "group2"))
        cli.XGroupCreate("x-stream", "group2", MkStream: true);
}

if (!cli.Exists("x-stream-claim"))
{
    cli.XGroupCreate("x-stream-claim", "group1", MkStream: true);
}
else
{
    //多个分组表示消息会分发到多个分组, 消息也会被消费多次
    var infoGroups = cli.XInfoGroups("x-stream-claim");
    if (!infoGroups.Any(x => x.name == "group1"))
        cli.XGroupCreate("x-stream-claim", "group1", MkStream: true);
}

var pendingResult = cli.XPending("x-stream", "group1");

//var p = cli.XPending("x-stream", "group1", "-", "+", 100000, "subscriber-1");

//cli.XAck("x-stream", "group1", "1654697774982-0");
//cli.XDel("x-stream", "1654697774982-0");

//var data = cli.XClaim("x-stream", "group1", "subscriber-2", 3600, p.Select(x => x.id).ToArray());

//var p2 = cli.XPending("x-stream", "group1", "-", "+", 1000000, "subscriber-2");

while (true)
{
    var str = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(str))
    {
        // var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(new TestMessage
        // {
        //     Input = str,
        // }));
        // cli.XAdd("x-stream", dic);

        testPublisher.PublishAsync(new TestMessage()
        {
            Input = str
        }).ConfigureAwait(false).GetAwaiter();
    }
}