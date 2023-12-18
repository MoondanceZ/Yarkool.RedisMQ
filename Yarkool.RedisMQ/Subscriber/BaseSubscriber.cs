namespace Yarkool.RedisMQ
{
    public abstract class BaseSubscriber : ISubscriber
    {
        /// <summary>
        /// On Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task OnMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// On Error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task OnErrorAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}