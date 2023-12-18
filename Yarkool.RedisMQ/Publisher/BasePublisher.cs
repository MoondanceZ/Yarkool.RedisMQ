using FreeRedis;

namespace Yarkool.RedisMQ
{
    public abstract class BasePublisher : IPublisher
    {
        private readonly RedisClient _redisClient;
        private readonly string _queueName;
        private readonly ISerializer _serializer;

        public BasePublisher()
        {
            var queueConfig = IocContainer.GetService<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _redisClient = IocContainer.GetService<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));

            _serializer = queueConfig.Serializer;

            var queueAttr = GetType().GetCustomAttributes(typeof(QueueSubscriberAttribute), false).FirstOrDefault() as QueueSubscriberAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueSubscriberAttribute));

            _queueName = $"{queueConfig.RedisPrefix}{queueAttr.QueueName}";
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task PublishAsync<TMessage>(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            var data = _serializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(new BaseMessage()
            {
                MessageContent = message
            }));

            await _redisClient.XAddAsync(_queueName, data);
        }
    }
}