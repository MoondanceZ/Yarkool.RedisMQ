using FreeRedis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Yarkool.RedisMQ
{
    public abstract class BaseSubscriber<TMessage> : BackgroundService, ISubscriber where TMessage : BaseMessage
    {
        public IServiceProvider ServiceProvider { get; }
        
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;
        private readonly ISerializer _serializer;
        private readonly ErrorPublisher _errorPublisher;
        private readonly ILogger _logger;

        private readonly string _queueName;
        private readonly string _groupName;
        private readonly string _subscriberName;
        private readonly int _subscriberCount;
        
        public BaseSubscriber(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            
            _queueConfig = serviceProvider.GetRequiredService<QueueConfig>();
            _serializer = _queueConfig.Serializer;
            _redisClient = serviceProvider.GetRequiredService<RedisClient>();
            _errorPublisher = serviceProvider.GetRequiredService<ErrorPublisher>();
            _logger = serviceProvider.GetRequiredService<ILogger<BaseSubscriber<TMessage>>>();

            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            _queueName = $"{_queueConfig.RedisPrefix}{queueAttr.QueueName}";
            _groupName = $"{queueAttr.QueueName}_Group";
            _subscriberName = $"{queueAttr.QueueName}_Subscriber";
            _subscriberCount = queueAttr.SubscriberCount;

            if (_subscriberCount <= 0)
                throw new ArgumentException("Queue subscriber count should > 0");

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await SubscribeAsync(stoppingToken);
        }

        public Task SubscribeAsync(CancellationToken cancellationToken)
        {
            for (var i = 0; i < _subscriberCount; i++)
            {
                var subscriberIndex = i + 1;

                var actualQueueName = string.IsNullOrEmpty(_queueConfig.RedisPrefix) ? _queueName : _queueName.Replace(_queueConfig.RedisPrefix , "");
                _logger.LogInformation($"{actualQueueName} {_subscriberName}_{subscriberIndex} subscribing");

                Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var messageContent = string.Empty;
                        var data = _redisClient.XReadGroup(_groupName, $"{_subscriberName}_{subscriberIndex}", 100, _queueName, ">");
                        if (data != null)
                        {
                            try
                            {
                                var message = data.fieldValues.MapToClass<TMessage>(encoding: Encoding.UTF8);
                                messageContent = _serializer.Serialize(message);

                                //Execute message
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
                }, cancellationToken);
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