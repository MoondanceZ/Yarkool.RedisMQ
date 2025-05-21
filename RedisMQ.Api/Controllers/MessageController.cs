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
    public string PublishMessage()
    {
        for (int i = 0; i < 10; i++)
        {
            _ = _publisher.PublishMessageAsync("Test", new TestMessage
            {
                Input = i.ToString(),
                MessageBody = new TestMessage.TestMessageBody()
            });
        }

        return "success";
    }

    /// <summary>
    /// 发送延迟队列
    /// </summary>
    /// <returns></returns>
    [HttpPost("PublishDelayMessage")]
    public async Task<string> PublishDelayMessage(int delaySeconds = 10)
    {
        var input = Guid.NewGuid().ToString("N");

        for (int i = 0; i < 10; i++)
        {
            _ = await _publisher.PublishMessageAsync("Delay", new TestMessage
            {
                Input = input,
                MessageBody = new TestMessage.TestMessageBody()
            }, TimeSpan.FromSeconds(delaySeconds));

            _ = await _publisher.PublishMessageAsync("Delay2", new TestMessage
            {
                Input = input,
                MessageBody = new TestMessage.TestMessageBody()
            }, TimeSpan.FromSeconds(delaySeconds));

            _ = await _publisher.PublishMessageAsync("Delay3", new TestMessage
            {
                Input = input,
                MessageBody = new TestMessage.TestMessageBody()
            }, TimeSpan.FromSeconds(delaySeconds));

            _ = await _publisher.PublishMessageAsync("Delay4", new TestMessage
            {
                Input = input,
                MessageBody = new TestMessage.TestMessageBody()
            }, TimeSpan.FromSeconds(delaySeconds));
        }

        return $"success";
    }
}