using FreeRedis;

namespace Yarkool.RedisMQ;

public abstract class RedisMQConsumer<TMessage>
{
    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task OnMessageAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="consumerReceivedDescriptor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task OnMessageAsync(TMessage message, ConsumerReceivedDescriptor consumerReceivedDescriptor, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// On Error
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task OnErrorAsync(TMessage message, Exception ex, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}