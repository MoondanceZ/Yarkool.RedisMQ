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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerBackgroundService> _logger;

    public ConsumerBackgroundService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, CacheKeyManager cacheKeyManager, IRedisClient redisClient, IServiceProvider serviceProvider, ILogger<ConsumerBackgroundService> logger)
    {
        _consumerServiceSelector = consumerServiceSelector;
        _queueConfig = queueConfig;
        _redisClient = redisClient;
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
            var automaticRetryAttempts = consumerExecutorDescriptor.AutomaticRetryAttempts;
            var isDelayQueueConsumer = consumerExecutorDescriptor.IsDelayQueueConsumer;
            var prefetchCount = consumerExecutorDescriptor.PrefetchCount;
            var isAutoAck = consumerExecutorDescriptor.IsAutoAck;
            var onMessageAsyncMethod = consumerType.GetMethod(nameof(RedisMQConsumer<object>.OnMessageAsync))!;
            var onMessageAsyncMethodInvoker = MethodInvoker.Create(onMessageAsyncMethod);
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
                    _logger.LogInformation($"{curConsumerName} subscribing");
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

                                            using var pipe = _redisClient.StartPipe();
                                            pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                            pipe.HSet($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Processing.ToString());
                                            pipe.ZAdd(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                            pipe.EndPipe();

                                            //Execute message
                                            await ((Task)onMessageAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, stoppingToken)!).ConfigureAwait(false);

                                            using var tran = _redisClient.Multi();
                                            tran.IncrBy($"{_cacheKeyManager.ConsumeSucceeded}:Total", 1);
                                            tran.IncrBy($"{_cacheKeyManager.ConsumeSucceeded}:{time}", 1);
                                            tran.Expire($"{_cacheKeyManager.ConsumeSucceeded}:{time}", TimeSpan.FromHours(30));
                                            tran.HSet($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "Status", MessageStatus.Completed.ToString());
                                            tran.ZAdd(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                            tran.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                            tran.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);
                                            tran.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                            tran.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), message.MessageId);
                                            if (isAutoAck)
                                            {
                                                //ACK
                                                tran.XAck(queueNameKey, groupName, data.id);
                                                tran.XDel(queueNameKey, data.id);
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
                                                //Execute message
                                                if (onErrorAsyncMethodInvoker != null)
                                                {
                                                    var consumer = _serviceProvider.CreateScope().ServiceProvider.GetService(consumerType);
                                                    await ((Task)onErrorAsyncMethodInvoker.Invoke(consumer, messageContent, messageHandler, ex, stoppingToken)!).ConfigureAwait(false);
                                                }
                                            }
                                            catch (Exception errorEx)
                                            {
                                                _logger?.LogError(new RedisMQDataException("Handle error exception!", message, errorEx), "Handle error exception!");
                                            }

                                            try
                                            {
                                                using var pipe = _redisClient.StartPipe();
                                                pipe.IncrBy($"{_cacheKeyManager.ConsumeFailed}:Total", 1);
                                                pipe.IncrBy($"{_cacheKeyManager.ConsumeFailed}:{time}", 1);
                                                pipe.Expire($"{_cacheKeyManager.ConsumeFailed}:{time}", TimeSpan.FromHours(30));

                                                if (message != null)
                                                {
                                                    pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), message.MessageId);
                                                    pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), message.MessageId);

                                                    //超出重试次数, 从原来的队列中删除, 并加入到异常列表
                                                    var messageErrorInfo = default(MessageErrorInfo);
                                                    var errorInfoStr = _redisClient.HGet<string>($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "ErrorInfo");
                                                    if (errorInfoStr != null)
                                                    {
                                                        messageErrorInfo = _queueConfig.Serializer.Deserialize<MessageErrorInfo>(errorInfoStr);
                                                        messageErrorInfo!.RetryCount += 1;
                                                    }
                                                    else
                                                    {
                                                        messageErrorInfo = new MessageErrorInfo
                                                        {
                                                            ConsumerName = consumerName,
                                                            ExceptionMessage = ex.Message,
                                                            StackTrace = ex.StackTrace,
                                                            GroupName = groupName,
                                                            QueueName = queueName,
                                                            ErrorMessageContent = messageContent,
                                                            ErrorMessageTimestamp = TimeHelper.GetMillisecondTimestamp(),
                                                            RetryCount = 1
                                                        };
                                                    }

                                                    //超出了重试次数, 则删除队列消息, 并添加到错误列表
                                                    if (messageErrorInfo.RetryCount > automaticRetryAttempts)
                                                    {
                                                        pipe.XAck(queueNameKey, groupName, data.id);
                                                        pipe.XDel(queueNameKey, data.id);
                                                        pipe.ZRem(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), message.MessageId);
                                                        pipe.ZAdd(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), TimeHelper.GetMillisecondTimestamp(), _queueConfig.Serializer.Serialize(message));
                                                    }
                                                    else
                                                    {
                                                        pipe.ZAdd(_cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), TimeHelper.GetMillisecondTimestamp(), message.MessageId);
                                                        pipe.HSet($"{_cacheKeyManager.PublishMessageList}:{message.MessageId}", "ErrorInfo", _queueConfig.Serializer.Serialize(messageErrorInfo));
                                                    }

                                                    pipe.EndPipe();
                                                }
                                            }
                                            catch (Exception errorEx)
                                            {
                                                _logger?.LogError(new RedisMQDataException("Handle error data exception!", message, errorEx), "Handle error data exception!");
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
                        _redisClient.HSet($"{_cacheKeyManager.PublishMessageList}:{baseMessage.MessageId}", "Id", messageId);
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
            while (!stoppingToken.IsCancellationRequested)
            {
                await _redisClient.HSetAsync(_cacheKeyManager.ServerNodes, _serverName, DateTime.Now).ConfigureAwait(false);

                var serverNodes = await _redisClient.HGetAllAsync<DateTime>(_cacheKeyManager.ServerNodes).ConfigureAwait(false);
                var expireNodes = serverNodes.Where(x => x.Value < DateTime.Now.AddMinutes(-2));
                var consumerList = await _redisClient.SMembersAsync(_cacheKeyManager.ConsumerList).ConfigureAwait(false);
                var expireConsumerList = consumerList.Where(x => !serverNodes.Any(s => x.StartsWith($"{s.Key}:"))).ToList();
                if (expireNodes.Any())
                {
                    foreach (var node in expireNodes)
                    {
                        _redisClient.HDel(_cacheKeyManager.ServerNodes, node.Key);
                        expireConsumerList.AddRange(consumerList.Where(x => x.StartsWith($"{node.Key}:")));
                    }
                }

                if (expireConsumerList.Any())
                {
                    await _redisClient.SRemAsync(_cacheKeyManager.ConsumerList, expireConsumerList.Select(x => (object)x).ToArray()).ConfigureAwait(false);
                }

                await Task.Delay(20000, stoppingToken).ConfigureAwait(false);
            }
        }, stoppingToken);
    }
}