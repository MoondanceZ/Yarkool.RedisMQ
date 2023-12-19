using FreeRedis;

namespace Yarkool.RedisMQ
{
    public class RedisMQPublisher
    (
        QueueConfig queueConfig,
        RedisClient redisClient
    ) : IRedisMQPublisher
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<string> PublishAsync(string queueName, object message)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new RedisMQException("queue name cannot be null!");

            queueName = string.IsNullOrEmpty(queueConfig.RedisPrefix) ? queueName : $"{queueConfig.RedisPrefix}{queueName}";
            var baseMessage = new BaseMessage
            {
                MessageContent = message
            };
            var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));

            return await redisClient.XAddAsync(queueName, data);
        }
    }
}