using System.Reflection;

namespace Yarkool.RedisMQ;

public class ConsumerServiceSelector
{
    private readonly List<ConsumerExecutorDescriptor> _cacheList;

    public ConsumerServiceSelector(QueueConfig queueConfig, CacheKeyManager cacheKeyManager)
    {
        _cacheList = new List<ConsumerExecutorDescriptor>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var consumerTypes = assemblies.SelectMany(x => x.GetTypes())
            .Where(t => t is { IsAbstract: false, IsClass: true, BaseType.IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(RedisMQConsumer<>))
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            var messageTypeInfo = consumerType.BaseType!.GetGenericArguments()[0].GetTypeInfo();
            var queueConsumerAttribute = consumerType.GetCustomAttributes(typeof(RedisMQConsumerAttribute), false).FirstOrDefault() as RedisMQConsumerAttribute;
            if (queueConsumerAttribute == null)
                throw new RedisMQException($"{consumerType.Name} doesn't have a `RedisMQConsumerAttribute`!");
            if (string.IsNullOrEmpty(queueConsumerAttribute.QueueName))
                throw new RedisMQException($"{consumerType.Name}'s `RedisMQConsumerAttribute` queue name is null or empty!");

            var queueName = cacheKeyManager.ParseCacheKey(queueConsumerAttribute.QueueName);
            var groupName = $"{queueConsumerAttribute.QueueName}_Group";

            if (_cacheList.Any(x => x.QueueName == queueName))
                throw new RedisMQException($"Cannot add queue `{queueName}` repeatedly!");

            _cacheList.Add(new ConsumerExecutorDescriptor
            {
                ConsumerTypeInfo = consumerType.GetTypeInfo(),
                MessageTypeInfo = messageTypeInfo,
                QueueName = queueName,
                GroupName = groupName,
                IsDelayQueueConsumer = queueConsumerAttribute.IsDelayQueueConsumer,
                PendingTimeOut = queueConsumerAttribute.PendingTimeOut,
                RedisMQConsumerAttribute = queueConsumerAttribute,
                PrefetchCount = queueConsumerAttribute.PrefetchCount,
                IsAutoAck = queueConsumerAttribute.IsAutoAck
            });
        }
    }

    public IEnumerable<ConsumerExecutorDescriptor> GetConsumerExecutorDescriptors()
    {
        return _cacheList;
    }
}