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
            var isDelayQueueConsumer = consumerExecutorDescriptor.IsDelayQueueConsumer;
            var onMessageAsyncMethod = consumerType.GetMethod(nameof(IRedisMQConsumer<object>.OnMessageAsync))!;
            var onErrorAsyncMethod = consumerType.GetMethod(nameof(IRedisMQConsumer<object>.OnErrorAsync))!;

            if (isDelayQueueConsumer)
                Task.Run(() => ExecuteDelayQueuePollingAsync(queueName, stoppingToken), stoppingToken);

            for (var i = 0; i < consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount; i++)
            {
                var consumerIndex = i + 1;
                var curConsumerName = $"{consumerName}_{consumerIndex}";
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
                            var data = await _redisClient.XReadGroupAsync(groupName, curConsumerName, 5 * 1000, queueName, ">");
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

                                    if (isDelayQueueConsumer)
                                    {
                                        var messageIdHSetName = $"{queueName}:MessageId";
                                        await _redisClient.HDelAsync(messageIdHSetName, message.MessageId);
                                    }
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
                                            await _publisher.PublishMessageAsync(_queueConfig.ErrorQueueName, errorMessage);
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
                            await Task.Delay(30, stoppingToken);
                        }
                    }
                }, stoppingToken);
            }
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteDelayQueuePollingAsync(string queueName, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delayTimeSortedSetName = $"{queueName}:DelayTimeType";
            var delaySecondsSet = _redisClient.SMembers<double>(delayTimeSortedSetName);
            if (delaySecondsSet != null && delaySecondsSet.Any())
            {
                await Parallel.ForEachAsync(delaySecondsSet, stoppingToken, async (delaySeconds, _) =>
                {
                    var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";
                    var value = await _redisClient.ZRangeByScoreAsync(delayQueueName, 0, TimeHelper.GetMillisecondTimestamp());
                    if (value != null && value.Any())
                    {
                        foreach (var item in value)
                        {
                            var baseMessage = _queueConfig.Serializer.Deserialize<BaseMessage>(item ?? "");
                            if (baseMessage == null)
                                continue;

                            var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(baseMessage));
                            var queueMessageId = await _redisClient.XAddAsync(queueName, data);
                            var messageIdHSetName = $"{queueName}:MessageId";
                            await _redisClient.HSetAsync(messageIdHSetName, baseMessage.MessageId, new MessageModel
                            {
                                QueueName = queueName,
                                DelayQueueName = delayQueueName,
                                Status = MessageStatus.Processing,
                                Message = baseMessage,
                                QueueMessageId = queueMessageId // use to delete message
                            });
                            await _redisClient.ZRemAsync(delayQueueName, item);
                        }
                    }
                });
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}