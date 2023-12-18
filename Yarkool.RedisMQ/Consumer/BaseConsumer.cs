namespace Yarkool.RedisMQ
{
    public abstract class BaseConsumer<TMessage> : IConsumer
    {
        /// <summary>
        /// On Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task OnMessageAsync(TMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// On Error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task OnErrorAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}