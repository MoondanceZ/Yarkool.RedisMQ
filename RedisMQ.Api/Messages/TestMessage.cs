namespace RedisMQ.Api.Messages;

public class TestMessage
{
    public string? Input { get; set; }

    public TestMessageBody? MessageBody { get; set; }

    public class TestMessageBody
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}