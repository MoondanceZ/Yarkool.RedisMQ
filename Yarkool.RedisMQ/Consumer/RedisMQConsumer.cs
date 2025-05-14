namespace Yarkool.RedisMQ;

public abstract class RedisMQConsumer<TMessage>
{
    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="messageHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task OnMessageAsync(TMessage message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken = default);

    /// <summary>
    /// On Error
    /// </summary>
    /// <param name="message"></param>
    /// <param name="messageHandler"></param>
    /// <param name="ex"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task OnErrorAsync(TMessage message, ConsumerMessageHandler messageHandler, Exception ex, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}