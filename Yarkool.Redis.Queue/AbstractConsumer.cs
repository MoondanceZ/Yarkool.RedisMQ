using FreeRedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarkool.Redis.Queue.Utils;

namespace Yarkool.Redis.Queue
{
    public abstract class AbstractConsumer<TMessage> where TMessage : BaseMessage
    {
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;

        public AbstractConsumer()
        {
            var queueConfig = IocContainer.Resolve<QueueConfig>();
            ArgumentNullException.ThrowIfNull(queueConfig, nameof(QueueConfig));

            var redisClient = IocContainer.Resolve<RedisClient>();
            ArgumentNullException.ThrowIfNull(redisClient, nameof(RedisClient));

            _queueConfig = queueConfig;
            _redisClient = redisClient;
        }

        public Task SubcribeAsync()
        {
            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            var queueName = $"{_queueConfig.RedisOptions.Prefix}{queueAttr.QueueName}";
            var groupName = $"{queueAttr.QueueName}_Group";
            var consumerName = $"{queueAttr.QueueName}_Consumer";
            for (var i = 0; i < queueAttr.ConsumerCount; i++)
            {
                var consumerIndex = i + 1;
                Task.Run(async () =>
                {
                    while (true)
                    {
                        //初始化队列信息
                        if (!_redisClient.Exists(queueName))
                        {
                            _redisClient.XGroupCreate(queueName, groupName, MkStream: true);
                        }
                        else
                        {
                            var infoGroups = _redisClient.XInfoGroups(queueName);
                            if (!infoGroups.Any(x => x.name == groupName))
                                _redisClient.XGroupCreate(queueName, groupName, MkStream: true);
                        }

                        var data = _redisClient.XReadGroup(groupName, $"{consumerName}_{consumerIndex}", 5, queueName, ">");
                        if (data != null)
                        {
                            try
                            {
                                var message = data.fieldValues.MapToClass<TMessage>(encoding: Encoding.UTF8);
                                await OnMessageAsync(message);

                                //ACK
                                _redisClient.XAck(queueName, groupName, data.id);
                                _redisClient.XDel(queueName, data.id);
                            }
                            catch (Exception e)
                            {
                                if (_queueConfig.ErrorQueueOptions != null)
                                {
                                }

                                await OnErrorAsync();
                            }
                        }
                    }
                    // ReSharper disable once FunctionNeverReturns
                });
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        protected abstract Task OnMessageAsync(TMessage message);

        /// <summary>
        /// 发生错误
        /// </summary>
        /// <returns></returns>
        protected abstract Task OnErrorAsync();
    }
}