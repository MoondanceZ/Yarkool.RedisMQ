// See https://aka.ms/new-console-template for more information
using FreeRedis;
using Newtonsoft.Json;

Console.WriteLine("Hello, World!");

var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
//cli.Notice += (s, e) => Console.WriteLine(e.Log);
while (true)
{
    //多个消费者消费同一个消费组, 消息会随机分布到消费者手上
    ThreadPool.QueueUserWorkItem(_ =>
    {
        var data = cli.XReadGroup("group1", "consumer-1", 5, "x-stream", ">");
        if (data != null)
            Console.WriteLine("consumer-1 receive message" + JsonConvert.SerializeObject(data));
    });

    ThreadPool.QueueUserWorkItem(_ =>
    {
        var data = cli.XReadGroup("group1", "consumer-2", 5, "x-stream", ">");
        if (data != null)
            Console.WriteLine("consumer-2 receive message" + JsonConvert.SerializeObject(data));
    });
}