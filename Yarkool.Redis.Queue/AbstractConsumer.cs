using FreeRedis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarkool.Redis.Queue
{
    public abstract class AbstractConsumer<TMessage> where TMessage : BaseMessage
    {
        private readonly QueueConfig _queueConfig;
        private readonly RedisClient _redisClient;
        private readonly ErrorProducer _errorProducer;
        private readonly ILogger<AbstractConsumer<TMessage>> _logger;

        private readonly string _queueName;
        private readonly string _groupName;
        private readonly string _consumerName;
        private readonly int _consumerCount;

        public AbstractConsumer()
        {
            _queueConfig = IocContainer.Resolve<QueueConfig>() ?? throw new ArgumentNullException(nameof(QueueConfig));
            _redisClient = IocContainer.Resolve<RedisClient>() ?? throw new ArgumentNullException(nameof(RedisClient));
            _errorProducer = IocContainer.Resolve<ErrorProducer>() ?? throw new ArgumentNullException(nameof(ErrorProducer));
            _logger = IocContainer.Resolve<ILogger<AbstractConsumer<TMessage>>>() ?? throw new ArgumentNullException(nameof(ErrorProducer));

            var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
            ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

            _queueName = $"{_queueConfig.RedisPrefix}{queueAttr.QueueName}";
            _groupName = $"{queueAttr.QueueName}_Group";
            _consumerName = $"{queueAttr.QueueName}_Consumer";
            _consumerCount = queueAttr.ConsumerCount;

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

        public Task SubcribeAsync()
        {
            for (var i = 0; i < _consumerCount; i++)
            {
                var consumerIndex = i + 1;
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var messageContent = string.Empty;
                        var data = _redisClient.XReadGroup(_groupName, $"{_consumerName}_{consumerIndex}", 5, _queueName, ">");
                        if (data != null)
                        {
                            try
                            {
                                var message = data.fieldValues.MapToClass<TMessage>(encoding: Encoding.UTF8);
                                messageContent = JsonConvert.SerializeObject(message);

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
                                            ConsumerName = _consumerName,
                                            ExceptionMessage = ex.Message,
                                            StackTrace = ex.StackTrace,
                                            GroupName = _groupName,
                                            MessageContent = messageContent,
                                            QueueName = _queueName,
                                        };
                                        await _errorProducer.PublishAsync(errorMessage);

                                    }

                                    await OnErrorAsync();
                                }
                                catch(Exception errorEx)
                                {
                                    _logger?.LogError(errorEx, "Handle error exception!");
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