using FreeRedis;

namespace Yarkool.RedisMQ
{
    public class RedisMQPublisher
    (
        QueueConfig queueConfig,
        CacheKeyManager cacheKeyManager,
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
            var time = DateTime.Now.ToString("yyyyMMddHH");
            try
            {
                if (string.IsNullOrEmpty(queueName))
                    throw new RedisMQException("queue name cannot be null!");

                queueName = cacheKeyManager.ParseCacheKey(queueName);
                var baseMessage = new BaseMessage { MessageContent = message == null ? null : queueConfig.Serializer.Serialize(message) };
                var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));
                var messageId = await redisClient.XAddAsync(queueName, data).ConfigureAwait(false);

                var pipe = redisClient.StartPipe();
                pipe.HSet(cacheKeyManager.MessageIdMapping, baseMessage.MessageId, messageId);
                pipe.IncrBy($"{cacheKeyManager.PublishSucceeded}:Total", 1);
                pipe.IncrBy($"{cacheKeyManager.PublishSucceeded}:{time}", 1);
                pipe.Expire($"{cacheKeyManager.PublishSucceeded}:{time}", TimeSpan.FromHours(30));
                pipe.EndPipe();

                return baseMessage.MessageId;
            }
            catch
            {
                var pipe = redisClient.StartPipe();
                pipe.IncrBy($"{cacheKeyManager.PublishFailed}:Total", 1);
                pipe.IncrBy($"{cacheKeyManager.PublishFailed}:{time}", 1);
                pipe.Expire($"{cacheKeyManager.PublishFailed}:{time}", TimeSpan.FromHours(30));
                pipe.EndPipe();
                throw;
            }
        }

        /// <summary>
        /// publish delay message
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        public Task<string> PublishMessageAsync(string queueName, object? message, TimeSpan delayTime)
        {
            var time = DateTime.Now.ToString("yyyyMMddHH");
            try
            {
                if (string.IsNullOrEmpty(queueName))
                    throw new RedisMQException("queue name cannot be null!");
                queueName = cacheKeyManager.ParseCacheKey(queueName);

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
                        redisClient.SAddAsync(delayTimeSortedSetName, delaySeconds);
                        _delaySecondsList.Add(delaySeconds);
                    }
                }

                var baseMessage = new BaseMessage
                {
                    MessageContent = message == null ? null : queueConfig.Serializer.Serialize(message),
                    DelayTime = delaySeconds
                };
                var pipe = redisClient.StartPipe();
                pipe.ZAdd(delayQueueName, (decimal)score, queueConfig.Serializer.Serialize(baseMessage));
                pipe.IncrBy($"{cacheKeyManager.PublishSucceeded}:Total", 1);
                pipe.IncrBy($"{cacheKeyManager.PublishSucceeded}:{time}", 1);
                pipe.Expire($"{cacheKeyManager.PublishSucceeded}:{time}", TimeSpan.FromHours(30));
                pipe.SAdd(cacheKeyManager.QueueList, queueName);
                pipe.EndPipe();

                return Task.FromResult(baseMessage.MessageId);
            }
            catch
            {
                var pipe = redisClient.StartPipe();
                pipe.IncrBy($"{cacheKeyManager.PublishFailed}:Total", 1);
                pipe.IncrBy($"{cacheKeyManager.PublishFailed}:{time}", 1);
                pipe.Expire($"{cacheKeyManager.PublishFailed}:{time}", TimeSpan.FromHours(30));
                pipe.EndPipe();
                throw;
            }
        }
    }
}