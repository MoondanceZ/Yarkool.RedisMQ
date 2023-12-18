namespace Yarkool.RedisMQ;

public interface ISubscriber
{
    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task OnMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// On Error
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    Task OnErrorAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);
}