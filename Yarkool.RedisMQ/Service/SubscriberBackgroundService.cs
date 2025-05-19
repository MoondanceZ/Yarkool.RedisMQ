using System.Collections.Concurrent;
using System.Diagnostics;
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
    private readonly CacheKeyManager _cacheKeyManager;
    private readonly IRedisClient _redisClient;
    private readonly IRedisMQPublisher _publisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerBackgroundService> _logger;

    public ConsumerBackgroundService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, CacheKeyManager cacheKeyManager, IRedisClient redisClient, IRedisMQPublisher publisher, IServiceProvider serviceProvider, ILogger<ConsumerBackgroundService> logger)
    {
        _consumerServiceSelector = consumerServiceSelector;
        _queueConfig = queueConfig;
        _redisClient = redisClient;
        _publisher = publisher;
        _serviceProvider = serviceProvider;
        _cacheKeyManager = cacheKeyManager;
        _logger = logger;

        foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
        {
            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;
            var queueNameKey = cacheKeyManager.ParseCacheKey(queueName);

            //初始化队列信息
            if (!_redisClient.Exists(queueNameKey))
            {
                _redisClient.XGroupCreate(queueNameKey, groupName, MkStream: true);
            }
            else
            {
                var infoGroups = _redisClient.XInfoGroups(queueNameKey);
                if (!infoGroups.Any(x => x.name == groupName))
                    _redisClient.XGroupCreate(queueNameKey, groupName, MkStream: true);
            }
        }
    }

    private readonly string _serverName = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumerExecutorDescriptor in _consumerServiceSelector.GetConsumerExecutorDescriptors())
        {
            var consumerType = consumerExecutorDescriptor.ConsumerTypeInfo;
            var messageType = consumerExecutorDescriptor.MessageTypeInfo;
            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;
            var consumerName = consumerExecutorDescriptor.ConsumerName;
            var isDelayQueueConsumer = consumerExecutorDescriptor.IsDelayQueueConsumer;
            var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
            var isAutoAck = consumerExecutorDescriptor.IsAutoAck;
            var onMessageAsyncMethod = consumerType.GetMethod(nameof(RedisMQConsumer<object>.OnMessageAsync))!;
            var onMessageAsyncMethodInvoker = MethodInvoker.Create(onMessageAsyncMethod)!;
            var onErrorAsyncMethod = consumerType.GetMethod(nameof(RedisMQConsumer<object>.OnErrorAsync));
            var onErrorAsyncMethodInvoker = onErrorAsyncMethod == null ? null : MethodInvoker.Create(onErrorAsyncMethod);
            var queueNameKey = _cacheKeyManager.ParseCacheKey(queueName);

            if (isDelayQueueConsumer)
                Task.Run(() => ExecuteDelayQueuePollingAsync(consumerExecutorDescriptor, stoppingToken), stoppingToken);

            for (var i = 0; i < consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount; i++)
            {
                var consumerIndex = i + 1;
                var curConsumerName = consumerExecutorDescriptor.RedisMQConsumerAttribute.ConsumerCount > 1 ? $"{consumerName}:{consumerIndex}" : consumerName;
                _redisClient.SAdd(_cacheKeyManager.ConsumerList, $"{_serverName}:{curConsumerName}");
                Task.Run(async () =>
                {
                    _logger?.LogInformation($"{curConsumerName} subscribing");
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var message = default(BaseMessage);
                        var messageContent = default(object);
                        // >：读取最新的消息（尚未分配给某个 consumer 的消息）
                        try
                        {
                            var streamsEntryResults = await _redisClient.XReadGroupAsync(groupName, curConsumerName, prefetchCount, 5 * 1000, false, queueNameKey, ">").ConfigureAwait(false);
                            if (streamsEntryResults != null && streamsEntryResults.Length != 0)
                            {
                                var entryResultEntries = streamsEntryResults.First()?.entries;
                                if (entryResultEntries?.Length > 0)
                                {
                                    foreach (var data in entryResultEntries)
                                    {
                                        var messageHandler = new ConsumerMessageHandler(queueNameKey, groupName, _redisClient, _cacheKeyManager);
                                        var time = DateTime.Now.ToString("yyyyMMddHH");
                                        try
                                        {
                                            var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                            message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);
                                            messageHandler.MessageId = message.MessageId;

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} subscribing {message.MessageContent}");
                                            messageContent = string.IsNullOrEmpty(message.MessageContent) ? null : _queueConfig.Serializer.Deserialize(message.MessageContent, messageType);

                                            //Execute message
                                            await ((Task)onMessageAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, stoppingToken)!).ConfigureAwait(false);

                                            using var tran = _redisClient.Multi();
                                            tran.IncrBy($"{_cacheKeyManager.ConsumeSucceeded}:Total", 1);
                                            tran.IncrBy($"{_cacheKeyManager.ConsumeSucceeded}:{time}", 1);
                                            tran.Expire($"{_cacheKeyManager.ConsumeSucceeded}:{time}", TimeSpan.FromHours(30));
                                            if (isAutoAck)
                                            {
                                                //ACK
                                                tran.XAck(queueNameKey, groupName, data.id);
                                                tran.XDel(queueNameKey, data.id);
                                                tran.HDel(_cacheKeyManager.MessageIdMapping, message.MessageId);
                                                tran.IncrBy($"{_cacheKeyManager.AckCount}:Total", 1);
                                                tran.IncrBy($"{_cacheKeyManager.AckCount}:{time}", 1);
                                                tran.Expire($"{_cacheKeyManager.AckCount}:{time}", TimeSpan.FromHours(30));
                                            }

                                            tran.Exec();

                                            // _logger?.LogInformation($"{consumerName}_{consumerIndex} consume {message.MessageContent} successfully");
                                        }
                                        catch (Exception ex)
                                        {
                                            try
                                            {
                                                using var pipe = _redisClient.StartPipe();
                                                pipe.IncrBy($"{_cacheKeyManager.ConsumeFailed}:Total", 1);
                                                pipe.IncrBy($"{_cacheKeyManager.ConsumeFailed}:{time}", 1);
                                                pipe.Expire($"{_cacheKeyManager.ConsumeFailed}:{time}", TimeSpan.FromHours(30));
                                                pipe.EndPipe();

                                                //Execute message
                                                if (onErrorAsyncMethodInvoker != null)
                                                {
                                                    var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                                    await ((Task)onErrorAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, ex, stoppingToken)!).ConfigureAwait(false);
                                                }

                                                if (_queueConfig.ErrorQueueOptions != null && message != null)
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
                                                    await _publisher.PublishMessageAsync(_cacheKeyManager.ParseCacheKey(_queueConfig.ErrorQueueOptions.QueueName), errorMessage).ConfigureAwait(false);

                                                    //delete message
                                                    if (_queueConfig.ErrorQueueOptions.IsDeleteOriginalQueueMessage)
                                                    {
                                                        using var tran = _redisClient.Multi();
                                                        tran.XAck(queueNameKey, groupName, data.id);
                                                        tran.XDel(queueNameKey, data.id);
                                                        tran.HDel(_cacheKeyManager.MessageIdMapping, message.MessageId);
                                                        tran.Exec();
                                                    }
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
                            _logger?.LogError(ex, $"{curConsumerName} read message exception!");
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
        _ = RunLiveServerAsync(stoppingToken);
        var queueName = consumerExecutorDescriptor.QueueName;
        var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
        var queueNameKey = _cacheKeyManager.ParseCacheKey(queueName);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delayTimeSortedSetName = $"{queueNameKey}:DelayTimeType";
            var delaySecondsSet = _redisClient.SMembers<double>(delayTimeSortedSetName);
            if (delaySecondsSet != null && delaySecondsSet.Any())
            {
                var messageMemberBag = new ConcurrentBag<(string DelayQueueName, string Member, decimal Score)>();
                foreach (var delaySeconds in delaySecondsSet.AsParallel())
                {
                    var delayQueueName = $"{delayTimeSortedSetName}:{delaySeconds}";
                    var zMembers = await _redisClient.ZRangeByScoreWithScoresAsync(delayQueueName, 0, TimeHelper.GetMillisecondTimestamp(), 0, prefetchCount).ConfigureAwait(false);

                    foreach (var item in zMembers)
                    {
                        messageMemberBag.Add((delayQueueName, item.member, item.score));
                    }
                }

                var messageMemberList = messageMemberBag.OrderBy(x => x.Score).ToList();
                if (messageMemberList.Any())
                {
                    foreach (var item in messageMemberList.AsParallel())
                    {
                        var baseMessage = _queueConfig.Serializer.Deserialize<BaseMessage>(item.Member ?? "");
                        if (baseMessage == null)
                            continue;

                        var data = _queueConfig.Serializer.Deserialize<Dictionary<string, object>>(_queueConfig.Serializer.Serialize(baseMessage));
                        var messageId = await _redisClient.XAddAsync(queueNameKey, data).ConfigureAwait(false);

                        using var tran = _redisClient.Multi();
                        _redisClient.HSet(_cacheKeyManager.MessageIdMapping, baseMessage.MessageId, messageId);
                        _redisClient.ZRem(item.DelayQueueName, item.Member);
                        tran.Exec();
                    }

                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(3000, stoppingToken).ConfigureAwait(false);
                }
            }
            else
            {
                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private Task RunLiveServerAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var serverNodes = await _redisClient.HGetAllAsync<DateTime>(_cacheKeyManager.ServerNodes).ConfigureAwait(false);
            var expireNodes = serverNodes.Where(x => x.Value < DateTime.Now.AddMinutes(-2));
            if (expireNodes.Any())
            {
                var consumerList = _redisClient.SMembers<string>(_cacheKeyManager.ConsumerList);
                foreach (var node in expireNodes)
                {
                    _redisClient.HDel(_cacheKeyManager.ServerNodes, node.Key);
                    var expireConsumerList = consumerList.Where(x => x.StartsWith($"{node.Key}:"));
                    if (expireConsumerList.Any())
                    {
                        _redisClient.SRem(_cacheKeyManager.ConsumerList, expireConsumerList);
                    }
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await _redisClient.HSetAsync(_cacheKeyManager.ServerNodes, _serverName, DateTime.Now).ConfigureAwait(false);
                await Task.Delay(20000, stoppingToken).ConfigureAwait(false);
            }

            await _redisClient.HDelAsync(_cacheKeyManager.ServerNodes, _serverName).ConfigureAwait(false);
        }, stoppingToken);
    }
}