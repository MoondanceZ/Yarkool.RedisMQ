using FreeRedis;

namespace Yarkool.RedisMQ
{
    public abstract class BasePublisher<TMessage> : IPublisher
    {
        private readonly RedisClient _redisClient;
        private readonly string _queueName;
        private readonly ISerializer _serializer;

        public BasePublisher()
        {
            var queueConfig = IocContainer.GetService<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _redisClient = IocContainer.GetService<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));

            _serializer = queueConfig.Serializer;

            var type = GetType();
            var queueAttr = type.GetCustomAttributes(typeof(QueuePublisherAttribute), false).FirstOrDefault() as QueuePublisherAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueuePublisherAttribute));

            _queueName = $"{queueConfig.RedisPrefix}{queueAttr.QueueName}";
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<string> PublishAsync(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            var baseMessage = new BaseMessage
            {
                MessageContent = message
            };
            var data = _serializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(baseMessage));

           return await _redisClient.XAddAsync(_queueName, data);
        }
    }
}