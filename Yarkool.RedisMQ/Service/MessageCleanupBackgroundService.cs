using FreeRedis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ;

internal class MessageCleanupBackgroundService
(
    QueueConfig queueConfig,
    CacheKeyManager cacheKeyManager,
    IRedisClient redisClient,
    ILogger<MessageCleanupBackgroundService> logger
)
    : BackgroundService
{
    private const int CleanupIntervalSeconds = 60;
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(CleanupIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clean completed and failed message data exception!");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                    break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private Task CleanupAsync(CancellationToken stoppingToken)
    {
        var cleanupLock = redisClient.Lock(cacheKeyManager.MessageCleanupLock, CleanupIntervalSeconds - 5);
        if (cleanupLock == null)
            return Task.CompletedTask;

        try
        {
            CleanupStatus(MessageStatus.Completed, queueConfig.CompletedMessageMaxLength, stoppingToken);
            CleanupStatus(MessageStatus.Failed, queueConfig.FailedMessageMaxLength, stoppingToken);
            CleanupPendingOrphans(stoppingToken);
        }
        finally
        {
            cleanupLock.Unlock();
        }

        return Task.CompletedTask;
    }

    private void CleanupStatus(MessageStatus status, int maxLength, CancellationToken stoppingToken)
    {
        if (maxLength < 0)
            return;

        var statusKey = cacheKeyManager.GetStatusMessageIdSet(status);
        var removeCount = redisClient.ZCard(statusKey) - maxLength;
        var totalRemoved = 0L;

        while (removeCount > 0 && !stoppingToken.IsCancellationRequested)
        {
            var batchCount = (int)Math.Min(removeCount, BatchSize);
            var messageIds = redisClient.ZRange(statusKey, 0, batchCount - 1);
            if (messageIds is not { Length: > 0 })
                break;

            using var pipe = redisClient.StartPipe();
            foreach (var messageId in messageIds)
            {
                pipe.ZRem(cacheKeyManager.PublishMessageIdSet, messageId);
                pipe.ZRem(statusKey, messageId);
                pipe.Del($"{cacheKeyManager.PublishMessageList}:{messageId}");
            }

            pipe.EndPipe();
            removeCount -= messageIds.Length;
            totalRemoved += messageIds.Length;
        }

        if (totalRemoved > 0)
            logger.LogInformation("Cleaned {Count} {Status} messages, max length {MaxLength}.", totalRemoved, status, maxLength);
    }

    private void CleanupPendingOrphans(CancellationToken stoppingToken)
    {
        if (queueConfig.PendingMessageOrphanTimeout < TimeSpan.Zero)
            return;

        var statusKey = cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending);
        var maxScore = (decimal)(TimeHelper.GetMillisecondTimestamp() - queueConfig.PendingMessageOrphanTimeout.TotalMilliseconds);
        var messageIds = redisClient.ZRangeByScore(statusKey, 0, maxScore, 0, BatchSize);
        if (messageIds is not { Length: > 0 })
            return;

        var fixedCount = 0;
        var failedCount = 0;
        foreach (var messageId in messageIds)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            var messageKey = $"{cacheKeyManager.PublishMessageList}:{messageId}";
            var messageData = redisClient.HGetAll<string>(messageKey);
            if (messageData is not { Count: > 0 })
            {
                redisClient.ZRem(statusKey, messageId);
                continue;
            }

            if (!messageData.TryGetValue("Status", out var status) || status != MessageStatus.Pending.ToString())
            {
                if (Enum.TryParse<MessageStatus>(status, out var realStatus))
                {
                    MoveToStatusSet(messageId, realStatus);
                    fixedCount++;
                }
                else
                {
                    redisClient.ZRem(statusKey, messageId);
                }

                continue;
            }

            if (messageData.TryGetValue("Type", out var type) &&
                type == "Delay" &&
                (!messageData.TryGetValue("Id", out var delayStreamId) || string.IsNullOrWhiteSpace(delayStreamId)))
            {
                continue;
            }

            if (!messageData.TryGetValue("Id", out var streamMessageId) || string.IsNullOrWhiteSpace(streamMessageId))
            {
                MarkPendingOrphanAsFailed(messageId, messageKey, messageData, "Pending message has no stream id.");
                failedCount++;
                continue;
            }

            if (!messageData.TryGetValue("Message", out var messageJson))
                continue;

            var message = queueConfig.Serializer.Deserialize<BaseMessage>(messageJson);
            if (message == null)
                continue;

            var streamEntries = redisClient.XRange(cacheKeyManager.GetQueueName(message.QueueName), streamMessageId, streamMessageId, 1);
            if (streamEntries is { Length: > 0 })
                continue;

            MarkPendingOrphanAsFailed(messageId, messageKey, messageData, $"Pending stream entry {streamMessageId} does not exist.");
            failedCount++;
        }

        if (fixedCount > 0)
            logger.LogInformation("Fixed {Count} pending message status indexes.", fixedCount);
        if (failedCount > 0)
            logger.LogWarning("Marked {Count} pending orphan messages as failed.", failedCount);
    }

    private void MoveToStatusSet(string messageId, MessageStatus status)
    {
        using var pipe = redisClient.StartPipe();
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), messageId);
        pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(status), TimeHelper.GetMillisecondTimestamp(), messageId);
        pipe.EndPipe();
    }

    private void MarkPendingOrphanAsFailed(string messageId, string messageKey, Dictionary<string, string> messageData, string reason)
    {
        var errorInfo = new MessageErrorInfo
        {
            QueueName = GetMessageQueueName(messageData),
            ConsumerName = nameof(MessageCleanupBackgroundService),
            ExceptionMessage = reason,
            ErrorMessageContent = messageData.TryGetValue("Message", out var messageJson) ? messageJson : null,
            ErrorMessageTimestamp = TimeHelper.GetMillisecondTimestamp()
        };

        using var pipe = redisClient.StartPipe();
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Pending), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Processing), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Retrying), messageId);
        pipe.ZRem(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Completed), messageId);
        pipe.HSet(messageKey, "Status", MessageStatus.Failed, "ErrorInfo", queueConfig.Serializer.Serialize(errorInfo));
        pipe.ZAdd(cacheKeyManager.GetStatusMessageIdSet(MessageStatus.Failed), TimeHelper.GetMillisecondTimestamp(), messageId);
        pipe.EndPipe();
    }

    private string GetMessageQueueName(Dictionary<string, string> messageData)
    {
        if (!messageData.TryGetValue("Message", out var messageJson))
            return string.Empty;

        var message = queueConfig.Serializer.Deserialize<BaseMessage>(messageJson);
        return message?.QueueName ?? string.Empty;
    }
}
