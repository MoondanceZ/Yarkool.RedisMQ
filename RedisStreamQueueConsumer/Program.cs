// See https://aka.ms/new-console-template for more information

using FreeRedis;
using Microsoft.Extensions.Hosting;
using Yarkool.RedisMQ;

//var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
////cli.Notice += (s, e) => Console.WriteLine(e.Log);

//var services = new ServiceCollection();
//services.AddRedisMQ(cli, config =>
//{
//    config.UseErrorQueue = true;
//    config.RedisPrefix = "Test:";
//});

Console.WriteLine("Consumer init");

var build = new HostBuilder()
    .ConfigureServices(services =>
    {
        var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
        //cli.Notice += (s, e) => Console.WriteLine(e.Log);

        services.AddRedisMQ(cli, config =>
        {
            config.UseErrorQueue = true;
            config.RedisPrefix = "Test:";
            config.AutoInitConsumer = true;
            config.IsEnableRePublishTimeOutMessage = true;
        });
    });

build.RunConsoleAsync();

//services.AddTransient<TestConsumer>();

//using var buildServiceProvider = services.BuildServiceProvider();
//var serviceProvider = buildServiceProvider.CreateScope().ServiceProvider;
//var testConsumer = serviceProvider.GetService<TestConsumer>()!;

//testConsumer.SubcribeAsync().ConfigureAwait(false).GetAwaiter();

while (true)
{
    //testConsumer.SubcribeAsync().ConfigureAwait(false).GetAwaiter();
    //多个消费者消费同一个消费组, 消息会随机分布到消费者手上
    // ThreadPool.QueueUserWorkItem(_ =>
    // {
    //     var data = cli.XReadGroup("group1", "consumer-1", 5, "x-stream", ">");
    //     if (data != null)
    //         Console.WriteLine("consumer-1 receive message" + JsonConvert.SerializeObject(data));
    // });

    // ThreadPool.QueueUserWorkItem(_ =>
    // {
    //     var data = cli.XReadGroup("group1", "consumer-2", 5, "x-stream", "0");
    //     if (data != null)
    //     {
    //         Console.WriteLine("consumer-2 receive message" + JsonConvert.SerializeObject(data));
    //         cli.XAck("x-stream", "group1", data.id);
    //         cli.XDel("x-stream", data.id);
    //     }
    // });

    // var data = cli.XReadGroup("group1", "consumer-2", 1, 5, false, "x-stream", ">")?.FirstOrDefault()?.entries?.FirstOrDefault();
    // if (data != null)
    // {
    //     var zz = data.fieldValues.MapToClass<TestMessage>(encoding: System.Text.Encoding.UTF8);
    //     //var messageList = data.
    //     Console.WriteLine("consumer-2 receive message, id =" + data.id + zz.Input);
    //     cli.XAck("x-stream", "group1", data.id);
    //     cli.XDel("x-stream", data.id);
    // }

    //ThreadPool.QueueUserWorkItem(_ =>
    //{
    //    var data = cli.XReadGroup("group2", "consumer-2", 5, "x-stream", ">");
    //    if (data != null)
    //        Console.WriteLine("consumer-2 receive message" + JsonConvert.SerializeObject(data));
    //});
}