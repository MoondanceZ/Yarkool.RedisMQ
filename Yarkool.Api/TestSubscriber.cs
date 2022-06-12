using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    internal class TestSubscriber : BaseSubscriber<TestMessage>
    {
        protected override Task OnMessageAsync(TestMessage message)
        {
            Console.WriteLine(message.Input);
            return Task.CompletedTask;
        }

        protected override Task OnErrorAsync()
        {
            throw new NotImplementedException();
        }

        public TestSubscriber(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
