using System.Text.Json;
using System.Text.Json.Serialization;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    [QueueSubscriber("Test")]
    public class TestSubscriber : BaseSubscriber
    {
        private readonly ILogger<TestSubscriber> _logger;

        public TestSubscriber(ILogger<TestSubscriber> logger)
        {
            _logger = logger;
        }

        public override async Task OnMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine(JsonSerializer.Serialize(message));

            await Task.CompletedTask;
        }
    }
}