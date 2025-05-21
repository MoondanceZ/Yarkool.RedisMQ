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
                if (message == null)
                    throw new RedisMQException("message cannot be null!");

                var queueNameKey = cacheKeyManager.GetQueueName(queueName);
                var baseMessage = new BaseMessage
                {
                    QueueName = queueName,
                    MessageContent = queueConfig.Serializer.Serialize(message)
                };
                var data = queueConfig.Serializer.Deserialize<Dictionary<string, object>>(queueConfig.Serializer.Serialize(baseMessage));
                var messageId = await redisClient.XAddAsync(queueNameKey, data).ConfigureAwait(false);

                var tran = redisClient.Multi();
                tran.IncrBy($"{cacheKeyManager.PublishSucceeded}:Total", 1);
                tran.IncrBy($"{cacheKeyManager.PublishSucceeded}:{time}", 1);
                tran.Expire($"{cacheKeyManager.PublishSucceeded}:{time}", TimeSpan.FromHours(30));
                tran.SAdd(cacheKeyManager.CommonQueueList, queueName);
                tran.ZAdd(cacheKeyManager.PublishMessageIdSet, baseMessage.CreateTimestamp, baseMessage.MessageId);
                tran.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), baseMessage.CreateTimestamp, baseMessage.MessageId);
                tran.HSet($"{cacheKeyManager.PublishMessageList}:{baseMessage.MessageId}", new Dictionary<string, string>
                {
                    ["Type"] = "Common",
                    ["Status"] = MessageStatus.Pending.ToString(),
                    ["Message"] = queueConfig.Serializer.Serialize(baseMessage),
                    ["Id"] = messageId
                });
                tran.Exec();

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
                if (message == null)
                    throw new RedisMQException("message cannot be null!");

                var queueNameKey = cacheKeyManager.GetQueueName(queueName);

                var delaySeconds = delayTime.TotalSeconds;
                if (delaySeconds <= 0)
                    throw new RedisMQException("delay time cannot be <= 0s !");
                var score = TimeHelper.GetMillisecondTimestamp() + (delaySeconds * 1000);
                var delayTimeSortedSetName = $"{queueNameKey}:DelayTimeType";
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
                    QueueName = queueName,
                    MessageContent = queueConfig.Serializer.Serialize(message),
                    DelayTime = delaySeconds
                };
                var tran = redisClient.Multi();
                tran.ZAdd(delayQueueName, (decimal)score, queueConfig.Serializer.Serialize(baseMessage));
                tran.IncrBy($"{cacheKeyManager.PublishSucceeded}:Total", 1);
                tran.IncrBy($"{cacheKeyManager.PublishSucceeded}:{time}", 1);
                tran.Expire($"{cacheKeyManager.PublishSucceeded}:{time}", TimeSpan.FromHours(30));
                tran.SAdd(cacheKeyManager.DelayQueueList, queueName);
                tran.SAdd(cacheKeyManager.DelayQueueNameList, delayQueueName);
                tran.ZAdd(cacheKeyManager.PublishMessageIdSet, baseMessage.CreateTimestamp, baseMessage.MessageId);
                tran.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), baseMessage.CreateTimestamp, baseMessage.MessageId);
                tran.HSet($"{cacheKeyManager.PublishMessageList}:{baseMessage.MessageId}", new Dictionary<string, string>
                {
                    ["Type"] = "Delay",
                    ["Status"] = MessageStatus.Pending.ToString(),
                    ["Message"] = queueConfig.Serializer.Serialize(baseMessage)
                });
                tran.Exec();

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