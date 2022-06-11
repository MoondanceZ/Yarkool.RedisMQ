using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Yarkool.RedisMQ;

namespace Yarkool.Api
{
    internal class TestSubscriber : BaseSubscriber<TestMessage>
    {
        protected override Task OnMessageAsync(TestMessage message)
        {
            Console.WriteLine(JsonConvert.SerializeObject(message));
            return Task.CompletedTask;
        }

        protected override Task OnErrorAsync()
        {
            throw new NotImplementedException();
        }
    }
}
