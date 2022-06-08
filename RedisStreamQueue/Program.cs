// See https://aka.ms/new-console-template for more information

using FreeRedis;

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

    //if (!infoGroups.Any(x => x.name == "group2"))
    //    cli.XGroupCreate("x-stream", "group2", MkStream: true);
}

var pendingResult = cli.XPending("x-stream", "group1");

var p = cli.XPending("x-stream", "group1", "-", "+", 10);

while (true)
{
    var str = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(str))
    {
        cli.XAdd("x-stream", "msg", str);
    }
}