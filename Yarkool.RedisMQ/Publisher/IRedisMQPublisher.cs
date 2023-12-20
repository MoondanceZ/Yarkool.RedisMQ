namespace Yarkool.RedisMQ;

public interface IRedisMQPublisher
{
    /// <summary>
    /// publish message
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<string> PublishMessageAsync(string queueName, object? message);

    /// <summary>
    /// publish delay message
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="message"></param>
    /// <param name="delayTime"></param>
    /// <returns></returns>
    Task<string> PublishMessageAsync(string queueName, object? message, TimeSpan delayTime);
}