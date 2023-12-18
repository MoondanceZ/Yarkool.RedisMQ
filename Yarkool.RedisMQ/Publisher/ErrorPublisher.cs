namespace Yarkool.RedisMQ
{
    [QueuePublisher("Error")]
    public class ErrorPublisher : BasePublisher<ErrorMessage>
    {
    }
}