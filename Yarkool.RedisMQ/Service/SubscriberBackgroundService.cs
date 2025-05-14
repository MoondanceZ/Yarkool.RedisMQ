using System.Reflection;
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
    private readonly IRedisClient _redisClient;
    private readonly IRedisMQPublisher _publisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerBackgroundService> _logger;

    public ConsumerBackgroundService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, IRedisClient redisClient, IRedisMQPublisher publisher, IServiceProvider serviceProvider, ILogger<ConsumerBackgroundService> logger)
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
            var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
            var isAutoAck = consumerExecutorDescriptor.IsAutoAck;
            var onMessageAsyncMethod = consumerType.GetMethod(nameof(RedisMQConsumer<object>.OnMessageAsync))!;
            var onMessageAsyncMethodInvoker = MethodInvoker.Create(onMessageAsyncMethod)!;
            var onErrorAsyncMethod = consumerType.GetMethod(nameof(RedisMQConsumer<object>.OnErrorAsync));
            var onErrorAsyncMethodInvoker = onErrorAsyncMethod == null ? null : MethodInvoker.Create(onErrorAsyncMethod);

            if (isDelayQueueConsumer)
                Task.Run(() => ExecuteDelayQueuePollingAsync(consumerExecutorDescriptor, stoppingToken), stoppingToken);

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
                            var streamsEntryResults = await _redisClient.XReadGroupAsync(groupName, curConsumerName, prefetchCount, 5 * 1000, false, queueName, ">").ConfigureAwait(false);
                            if (streamsEntryResults != null && streamsEntryResults.Length != 0)
                            {
                                var entryResultEntries = streamsEntryResults.First()?.entries;
                                if (entryResultEntries?.Length > 0)
                                {
                                    foreach (var data in entryResultEntries)
                                    {
                                        var messageHandler = new ConsumerMessageHandler(queueName, groupName, _redisClient);
                                        try
                                        {
                                            var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                            message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);
                                            messageHandler.MessageId = message.MessageId;

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} subscribing {message.MessageContent}");
                                            messageContent = string.IsNullOrEmpty(message.MessageContent) ? null : _queueConfig.Serializer.Deserialize(message.MessageContent, messageType);

                                            //Execute message
                                            await ((Task)onMessageAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, stoppingToken)!).ConfigureAwait(false);

                                            if (isAutoAck)
                                            {
                                                //ACK use tran
                                                using var tran = _redisClient.Multi();
                                                tran.XAck(queueName, groupName, data.id);
                                                tran.XDel(queueName, data.id);
                                                tran.HDel(Constants.MessageIdMapping, message.MessageId);
                                                tran.Exec();
                                            }

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} consume {message.MessageContent} successfully");
                                        }
                                        catch (Exception ex)
                                        {
                                            try
                                            {
                                                //Execute message
                                                if (onErrorAsyncMethodInvoker != null)
                                                {
                                                    var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                                    await ((Task)onErrorAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, ex, stoppingToken)!).ConfigureAwait(false);
                                                }

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
                                                    await _publisher.PublishMessageAsync(_queueConfig.ErrorQueueName, errorMessage).ConfigureAwait(false);
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

    private async Task ExecuteDelayQueuePollingAsync(ConsumerExecutorDescriptor consumerExecutorDescriptor, CancellationToken stoppingToken)
    {
        var queueName = consumerExecutorDescriptor.QueueName;
        var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delayTimeSortedSetName = $"{queueName}:DelayTimeType";
            var delaySecondsSet = _redisClient.SMembers<double>(delayTimeSortedSetName);
            if (delaySecondsSet != null && delaySecondsSet.Any())
            {
                var messageMemberList = new List<(string DelayQueueName, string Member, decimal Score)>();
                foreach (var delaySeconds in delaySecondsSet)
                {
                    var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";
                    var zMembers = await _redisClient.ZRangeByScoreWithScoresAsync(delayQueueName, 0, TimeHelper.GetMillisecondTimestamp(), 0, prefetchCount).ConfigureAwait(false);

                    foreach (var item in zMembers)
                    {
                        messageMemberList.Add((delayQueueName, item.member, item.score));
                    }
                }

                messageMemberList = messageMemberList.OrderBy(x => x.Score).ToList();
                foreach (var item in messageMemberList)
                {
                    var baseMessage = _queueConfig.Serializer.Deserialize<BaseMessage>(item.Member ?? "");
                    if (baseMessage == null)
                        continue;

                    var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(baseMessage));
                    var messageId = await _redisClient.XAddAsync(queueName, data).ConfigureAwait(false);

                    using var tran = _redisClient.Multi();
                    _redisClient.HSet(Constants.MessageIdMapping, baseMessage.MessageId, messageId);
                    _redisClient.ZRem(item.DelayQueueName, item.Member);
                    tran.Exec();
                }
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}