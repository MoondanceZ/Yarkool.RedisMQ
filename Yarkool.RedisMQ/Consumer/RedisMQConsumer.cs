namespace Yarkool.RedisMQ;

internal interface IRedisMQConsumerExecutor
{
    Task ExecuteAsync(object? message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken);

    Task ExecuteErrorAsync(object? message, ConsumerMessageHandler messageHandler, Exception ex, CancellationToken cancellationToken);
}

public abstract class RedisMQConsumer<TMessage> : IRedisMQConsumerExecutor
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

    Task IRedisMQConsumerExecutor.ExecuteAsync(object? message, ConsumerMessageHandler messageHandler, CancellationToken cancellationToken)
    {
        return OnMessageAsync((TMessage)message!, messageHandler, cancellationToken);
    }

    Task IRedisMQConsumerExecutor.ExecuteErrorAsync(object? message, ConsumerMessageHandler messageHandler, Exception ex, CancellationToken cancellationToken)
    {
        return OnErrorAsync((TMessage)message!, messageHandler, ex, cancellationToken);
    }
}
