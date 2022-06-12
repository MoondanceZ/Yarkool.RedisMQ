using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    public class TestSubscriber : BaseSubscriber<TestMessage>
    {
        private readonly ILogger<TestSubscriber> _logger;

        protected override Task OnMessageAsync(TestMessage message)
        {
            _logger.LogInformation(message.Input);
            Console.WriteLine(message.Input);
            return Task.CompletedTask;
        }

        protected override Task OnErrorAsync()
        {
            throw new NotImplementedException();
        }

        public TestSubscriber(ILogger<TestSubscriber> logger)
        {
            _logger = logger;
        }
    }
}
