namespace Yarkool.RedisMQ;

public interface IRedisMQConsumer<in TMessage>
{
    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task OnMessageAsync(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// On Error
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task OnErrorAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}