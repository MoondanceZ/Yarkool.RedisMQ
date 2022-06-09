using FreeRedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    public abstract class AbstractConsumer<TMessage> where TMessage : BaseMessage
    {
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;

        public AbstractConsumer(QueueConfig queueConfig, RedisClient redisClient)
        {
            ArgumentNullException.ThrowIfNull(queueConfig, nameof(QueueConfig));
            ArgumentNullException.ThrowIfNull(redisClient, nameof(RedisClient));

            _queueConfig = queueConfig;
            _redisClient = redisClient;
        }

        public void Subcribe()
        {
            try
            {
                var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueQttribute), false).FirstOrDefault() as QueueQttribute;
                ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueQttribute));

                var queueName = $"{_queueConfig.RedisOptions.Prefix}{queueAttr.QueueName}";
                var groupName = $"{queueAttr.QueueName}_Group";
                var consumerName = $"{queueAttr.QueueName}_Consumer";
                for (int i = 0; i < queueAttr.ConsumerCount; i++)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        while (true)
                        {
                            var data = _redisClient.XReadGroup(groupName, $"{consumerName}_{i + 1}", 5, queueName, ">");
                            if (data != null)
                            {

                            }
                        }
                    });
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public abstract Action OnMessage();

        /// <summary>
        /// 发生错误
        /// </summary>
        /// <returns></returns>
        public abstract Action OnError();
    }
}
