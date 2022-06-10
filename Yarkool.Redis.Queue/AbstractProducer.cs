using FreeRedis;
using Newtonsoft.Json;

namespace Yarkool.Redis.Queue
{
    public abstract class AbstractProducer<TMessage> where TMessage : BaseMessage
    {
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;
        private readonly string _queueName;
        private readonly string _groupName;

        public AbstractProducer()
        {
            _queueConfig = IocContainer.Resolve<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _redisClient = IocContainer.Resolve<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));

            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            _queueName = $"{_queueConfig.RedisPrefix}{queueAttr.QueueName}";
            _groupName = $"{queueAttr.QueueName}_Group";
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<bool> PublishAsync(TMessage message)
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(message));

            _redisClient.XAdd(_queueName, data);

            return Task.FromResult(true);
        }
    }
}