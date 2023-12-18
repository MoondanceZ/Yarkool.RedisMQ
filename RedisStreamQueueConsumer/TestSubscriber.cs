using Newtonsoft.Json;
using Yarkool.RedisMQ;

namespace RedisStreamQueue
{
    [QueueSubscriber("Test")]
    internal class TestSubscriber : BaseSubscriber
    {
        // protected override async Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        // {
        //     Console.WriteLine(JsonConvert.SerializeObject(message));
        //     await Task.Delay(2000, cancellationToken);
        // }
        //
        // protected override Task OnErrorAsync(CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        public override async Task OnMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}