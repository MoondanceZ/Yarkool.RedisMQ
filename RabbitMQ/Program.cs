// See https://aka.ms/new-console-template for more information
using EasyNetQ;

Console.WriteLine("Hello, World!");

var bus = RabbitHutch.CreateBus("host=localhost");

ThreadPool.QueueUserWorkItem(async _ =>
{
    await bus.PubSub.SubscribeAsync<string>("my_subscription_id", async msg =>
    {
        await Task.Delay(3000);
        Console.WriteLine("Subcribe " + msg);
    });
});

while (true)
{
    var str = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(str))
    {
        await bus.PubSub.PublishAsync(str, "mdzz");
    }
}