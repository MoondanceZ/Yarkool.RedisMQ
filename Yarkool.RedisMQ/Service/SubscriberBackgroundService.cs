using System.Text;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ;

public class ConsumerBackgroundService : BackgroundService
{
    private readonly ConsumerServiceSelector _consumerServiceSelector;
    private readonly QueueConfig _queueConfig;
    private readonly RedisClient _redisClient;
    private readonly IRedisMQPublisher _publisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerBackgroundService> _logger;

    public ConsumerBackgroundService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, RedisClient redisClient, IRedisMQPublisher publisher, IServiceProvider serviceProvider, ILogger<ConsumerBackgroundService> logger)
    {
        _consumerServiceSelector = consumerServiceSelector;
        _queueConfig = queueConfig;
        _redisClient = redisClient;
        _publisher = publisher;
        _serviceProvider = serviceProvider;
        _logger = logger;

        foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
        {
            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;

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
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
        {
            var consumerType = consumerExecutorDescriptor.ConsumerTypeInfo;
            var messageType = consumerExecutorDescriptor.MessageTypeInfo;
            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;
            var consumerName = $"{consumerExecutorDescriptor.QueueName}_Consumer";
            var onMessageAsyncMethod = consumerType.GetMethod(nameof(IRedisMQConsumer<object>.OnMessageAsync))!;
            var onErrorAsyncMethod = consumerType.GetMethod(nameof(IRedisMQConsumer<object>.OnErrorAsync))!;

            for (var i = 0; i < consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount; i++)
            {
                var consumerIndex = i + 1;
                Task.Run(async () =>
                {
                    _logger?.LogInformation($"{consumerName.Replace(_queueConfig.RedisPrefix ?? "", "")}_{consumerIndex} subscribing");
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var message = default(BaseMessage);
                        var messageContent = default(object);
                        // >：读取最新的消息（尚未分配给某个 consumer 的消息）
                        try
                        {
                            var data = await _redisClient.XReadGroupAsync(groupName, $"{consumerName}_{consumerIndex}", 5 * 1000, queueName, ">");
                            if (data != null)
                            {
                                var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                ArgumentNullException.ThrowIfNull(consumer, nameof(consumer));
                                try
                                {
                                    message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);

                                    // _logger?.LogInformation($"{consumerName}_{consumerIndex} subscribing {message.MessageContent}");
                                    messageContent = _queueConfig.Serializer.Deserialize(message.MessageContent as string, messageType);

                                    //Execute message
                                    await (Task)onMessageAsyncMethod.Invoke(consumer, new[]
                                    {
                                        messageContent,
                                        stoppingToken
                                    })!;

                                    //ACK
                                    await _redisClient.XAckAsync(queueName, groupName, data.id);
                                    await _redisClient.XDelAsync(queueName, data.id);
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        //Execute message
                                        await (Task)onErrorAsyncMethod.Invoke(consumer, new[]
                                        {
                                            messageContent,
                                            stoppingToken
                                        })!;
                                        
                                        if (_queueConfig.UseErrorQueue && message != null)
                                        {
                                            var errorMessage = new ErrorMessage
                                            {
                                                ConsumerName = consumerName,
                                                ExceptionMessage = ex.Message,
                                                StackTrace = ex.StackTrace,
                                                GroupName = groupName,
                                                QueueName = queueName,
                                                ErrorMessageContent = messageContent,
                                                ErrorMessageTimestamp = message.CreateTimestamp
                                            };
                                            await _publisher.PublishAsync(_queueConfig.ErrorQueueName, errorMessage);
                                        }
                                    }
                                    catch (Exception errorEx)
                                    {
                                        _logger?.LogError(new RedisMQDataException("Handle error exception!", message, errorEx), "Handle error exception!");
                                    }

                                    _logger?.LogError(new RedisMQDataException("Handle consumer message exception!", message, ex), "Handle consumer message exception!");
                                }
                                finally
                                {
                                    message = null;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, $"{consumerName}_{consumerIndex} read message exception!");
                        }
                    }
                }, stoppingToken);
            }
        }

        return Task.CompletedTask;
    }
}