using Microsoft.AspNetCore.Mvc;
using Yarkool.RedisMQ;

namespace RedisMQ.Api.Controllers;

[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IRedisMQPublisher _publisher;

    public MessageController(IRedisMQPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <summary>
    /// 发送普通队列
    /// </summary>
    /// <returns></returns>
    [HttpPost("PublishMessage")]
    public async Task<string> PublishMessage()
    {
        for (int i = 0; i < 10000; i++)
        {
            await _publisher.PublishMessageAsync("Test", new TestMessage
            {
                Input = i.ToString()
            });
        }

        return "success";
    }

    /// <summary>
    /// 发送延迟队列
    /// </summary>
    /// <returns></returns>
    [HttpPost("PublishDelayMessage")]
    public async Task<string> PublishDelayMessage()
    {
        var input = Guid.NewGuid().ToString("N");
        var messageId = await _publisher.PublishMessageAsync("Delay", new TestMessage
        {
            Input = input
        }, TimeSpan.FromSeconds(10));
        return $"{messageId}-{input}";
    }
}