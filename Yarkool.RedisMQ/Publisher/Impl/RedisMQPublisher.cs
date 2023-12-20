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
            var baseMessage = new BaseMessage
            {
                MessageContent = message
            };
            var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));
            await redisClient.XAddAsync(queueName, data);

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
            var score = TimeHelper.GetMillisecondTimestamp() + delaySeconds * 1000;
            var delayTimeSortedSetName = $"{queueName}:DelayTimeType";
            var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";
            await redisClient.SAddAsync(delayTimeSortedSetName, delaySeconds);

            var baseMessage = new BaseMessage
            {
                MessageContent = message,
                DelayTime = delaySeconds
            };

            var messageIdHSetName = $"{queueName}:MessageId";
            await redisClient.HSetAsync(messageIdHSetName, baseMessage.MessageId, new MessageModel
            {
                QueueName = queueName,
                DelayQueueName = delayQueueName,
                Status = MessageStatus.Pending,
                Message = baseMessage
            });
            await redisClient.ZAddAsync(delayQueueName, (decimal)score, queueConfig.Serializer.Serialize(baseMessage));

            return baseMessage.MessageId;
        }
    }
}