using Yarkool.RedisMQ;

namespace RedisStreamQueue
{
    [QueueConsumer("Test")]
    internal class TestConsumer : BaseConsumer<TestMessage>
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

        public override Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}