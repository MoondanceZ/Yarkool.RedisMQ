// See https://aka.ms/new-console-template for more information

using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Yarkool.Redis.Queue;

var services = new ServiceCollection();
services.AddRedisQueue(config =>
{
    config.UseConsumeErrorQueue(options =>
    {
        options.QueueName = "ConsumeErrorQueue";
    });

    config.RedisOptions = new RedisOptions
    {
        Host = "localhost",
        Prefix = "Test:"
    };
});

Console.WriteLine("Hello, World!");

var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
cli.Notice += (s, e) => Console.WriteLine(e.Log);

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

//var p = cli.XPending("x-stream", "group1", "-", "+", 100000, "consumer-1");

//cli.XAck("x-stream", "group1", "1654697774982-0");
//cli.XDel("x-stream", "1654697774982-0");

//var data = cli.XClaim("x-stream", "group1", "consumer-2", 3600, p.Select(x => x.id).ToArray());

//var p2 = cli.XPending("x-stream", "group1", "-", "+", 1000000, "consumer-2");

while (true)
{
    var str = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(str))
    {
        cli.XAdd("x-stream", "msg", str);
    }
}