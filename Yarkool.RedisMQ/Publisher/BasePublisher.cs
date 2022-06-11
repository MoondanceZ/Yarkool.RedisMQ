using FreeRedis;

namespace Yarkool.RedisMQ
{
    public abstract class BasePublisher<TMessage> : IPublisher where TMessage : BaseMessage
    {
        private readonly RedisClient _redisClient;
        private readonly string _queueName;
        private readonly ISerializer _serializer;

        public BasePublisher()
        {
            var queueConfig = IocContainer.Resolve<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _redisClient = IocContainer.Resolve<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));

            _serializer = queueConfig.Serializer;

            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            _queueName = $"{queueConfig.RedisPrefix}{queueAttr.QueueName}";
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<bool> PublishAsync(TMessage message)
        {
            var data = _serializer.Deserialize<Dictionary<string, string>>(_serializer.Serialize(message));

            _redisClient.XAdd(_queueName, data);

            return Task.FromResult(true);
        }
    }
}