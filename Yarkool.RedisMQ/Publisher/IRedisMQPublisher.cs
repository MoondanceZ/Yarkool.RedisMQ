namespace Yarkool.RedisMQ;

public interface IRedisMQPublisher
{
    /// <summary>
    /// publish message
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<string> PublishAsync(string queueName, object message);
}