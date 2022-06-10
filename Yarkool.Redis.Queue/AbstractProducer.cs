using FreeRedis;
using Newtonsoft.Json;
using Yarkool.Redis.Queue.Utils;

namespace Yarkool.Redis.Queue;

public abstract class AbstractProducer<TMessage> where TMessage : BaseMessage
{
    private readonly QueueConfig _queueConfig;
    private readonly RedisClient _redisClient;

    public AbstractProducer()
    {
        var queueConfig = IocContainer.Resolve<QueueConfig>();
        ArgumentNullException.ThrowIfNull(queueConfig, nameof(QueueConfig));

        var redisClient = IocContainer.Resolve<RedisClient>();
        ArgumentNullException.ThrowIfNull(redisClient, nameof(RedisClient));

        _queueConfig = queueConfig;
        _redisClient = redisClient;
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<bool> PublishAsync(TMessage message)
    {
        var queueAttr = typeof(TMessage).GetCustomAttributes(typeof(QueueAttribute), false).FirstOrDefault() as QueueAttribute;
        ArgumentNullException.ThrowIfNull(queueAttr, nameof(QueueAttribute));

        var queueName = $"{_queueConfig.RedisOptions.Prefix}{queueAttr.QueueName}";
        var groupName = $"{queueAttr.QueueName}_Group";

        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(message));

        _redisClient.XAdd(queueName, groupName, data);

        return Task.FromResult(true);
    }
}