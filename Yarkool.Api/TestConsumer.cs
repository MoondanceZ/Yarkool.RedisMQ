using System.Text.Json;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    [QueueConsumer("Test", ConsumerCount = 2)]
    public class TestConsumer : BaseConsumer<TestMessage>
    {
        private readonly ILogger<TestConsumer> _logger;

        public TestConsumer(ILogger<TestConsumer> logger)
        {
            _logger = logger;
        }

        public override Task OnMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(JsonSerializer.Serialize(message));
            return Task.CompletedTask;
        }
    }
}