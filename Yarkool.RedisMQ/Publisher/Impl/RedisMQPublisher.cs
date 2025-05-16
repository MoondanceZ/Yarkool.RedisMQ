using FreeRedis;

namespace Yarkool.RedisMQ
{
    public class RedisMQPublisher
    (
        QueueConfig queueConfig,
        IRedisClient redisClient
    ) : IRedisMQPublisher
    {
        private static readonly object _lock = new();
        private static readonly List<double> _delaySecondsList = new();

        /// <summary>
        /// publish message
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<string> PublishMessageAsync(string queueName, object? message)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new RedisMQException("queue name cannot be null!");

            queueName = string.IsNullOrEmpty(queueConfig.RedisPrefix) ? queueName : $"{queueConfig.RedisPrefix}{queueName}";
            var baseMessage = new BaseMessage { MessageContent = message == null ? null : queueConfig.Serializer.Serialize(message) };
            var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));
            var messageId = await redisClient.XAddAsync(queueName, data);
            await redisClient.HSetAsync(CacheKeys.MessageIdMapping, baseMessage.MessageId, messageId);

            return baseMessage.MessageId;
        }

        /// <summary>
        /// publish delay message
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        public async Task<string> PublishMessageAsync(string queueName, object? message, TimeSpan delayTime)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new RedisMQException("queue name cannot be null!");
            queueName = string.IsNullOrEmpty(queueConfig.RedisPrefix) ? queueName : $"{queueConfig.RedisPrefix}{queueName}";

            var delaySeconds = delayTime.TotalSeconds;
            if (delaySeconds <= 0)
                throw new RedisMQException("delay time cannot be <= 0s !");
            var score = TimeHelper.GetMillisecondTimestamp() + (delaySeconds * 1000);
            var delayTimeSortedSetName = $"{queueName}:DelayTimeType";
            var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";

            lock (_lock)
            {
                if (!_delaySecondsList.Contains(delaySeconds))
                {
                    redisClient.SAdd(delayTimeSortedSetName, delaySeconds);
                    _delaySecondsList.Add(delaySeconds);
                }
            }

            var baseMessage = new BaseMessage
            {
                MessageContent = message == null ? null : queueConfig.Serializer.Serialize(message),
                DelayTime = delaySeconds
            };

            await redisClient.ZAddAsync(delayQueueName, (decimal)score, queueConfig.Serializer.Serialize(baseMessage));

            return baseMessage.MessageId;
        }
    }
}