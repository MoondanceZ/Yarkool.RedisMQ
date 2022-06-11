using FreeRedis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarkool.RedisMQ
{
    public abstract class BaseSubscriber<TMessage> : ISubscriber where TMessage : BaseMessage
    {
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;
        private readonly ISerializer _serializer;
        private readonly ErrorPublisher _errorPublisher;
        private readonly ILogger<BaseSubscriber<TMessage>> _logger;

        private readonly string _queueName;
        private readonly string _groupName;
        private readonly string _subscriberName;
        private readonly int _subscriberCount;

        public BaseSubscriber()
        {
            _queueConfig = IocContainer.Resolve<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _serializer = _queueConfig.Serializer;
            _redisClient = IocContainer.Resolve<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));            
            _errorPublisher = IocContainer.Resolve<ErrorPublisher>() ?? throw new ArgumentNullException(nameof(ErrorPublisher));
            _logger = IocContainer.Resolve<ILogger<BaseSubscriber<TMessage>>>() ?? throw new ArgumentNullException(nameof(ErrorPublisher));

            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            _queueName = $"{_queueConfig.RedisPrefix}{queueAttr.QueueName}";
            _groupName = $"{queueAttr.QueueName}_Group";
            _subscriberName = $"{queueAttr.QueueName}_Subscriber";
            _subscriberCount = queueAttr.SubscriberCount;

            //初始化队列信息
            if (!_redisClient.Exists(_queueName))
            {
                _redisClient.XGroupCreate(_queueName, _groupName, MkStream: true);
            }
            else
            {
                var infoGroups = _redisClient.XInfoGroups(_queueName);
                if (!infoGroups.Any(x => x.name == _groupName))
                    _redisClient.XGroupCreate(_queueName, _groupName, MkStream: true);
            }
        }

        public Task SubscribeAsync()
        {
            for (var i = 0; i < _subscriberCount; i++)
            {
                var subscriberIndex = i + 1;

                _logger.LogInformation($"{_queueName.Replace(_queueConfig.RedisPrefix ?? "", "")} {_subscriberName}_{subscriberIndex} subscribing");

                Task.Run(async () =>
                {
                    while (true)
                    {
                        var messageContent = string.Empty;
                        var data = _redisClient.XReadGroup(_groupName, $"{_subscriberName}_{subscriberIndex}", 5, _queueName, ">");
                        if (data != null)
                        {
                            try
                            {
                                var message = data.fieldValues.MapToClass<TMessage>(encoding: Encoding.UTF8);
                                messageContent = _serializer.Serialize(message);

                                //Execute messge
                                await OnMessageAsync(message);

                                //ACK
                                _redisClient.XAck(_queueName, _groupName, data.id);
                                _redisClient.XDel(_queueName, data.id);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    if (_queueConfig.UseErrorQueue)
                                    {
                                        var errorMessage = new ErrorMessage
                                        {
                                            SubscriberName = _subscriberName,
                                            ExceptionMessage = ex.Message,
                                            StackTrace = ex.StackTrace,
                                            GroupName = _groupName,
                                            MessageContent = messageContent,
                                            QueueName = _queueName,
                                        };
                                        await _errorPublisher.PublishAsync(errorMessage);
                                    }

                                    await OnErrorAsync();
                                }
                                catch (Exception errorEx)
                                {
                                    _logger.LogError(errorEx, "Handle error exception!");
                                }
                            }
                            finally
                            {
                                messageContent = string.Empty;
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