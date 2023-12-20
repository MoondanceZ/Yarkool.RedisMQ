using System.Reflection;

namespace Yarkool.RedisMQ;

public class ConsumerServiceSelector
{
    private readonly List<ConsumerExecutorDescriptor> _cacheList;

    public ConsumerServiceSelector(QueueConfig queueConfig)
    {
        _cacheList = new List<ConsumerExecutorDescriptor>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var consumerTypes = assemblies.SelectMany(x => x.GetTypes())
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRedisMQConsumer<>)))
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            var interfaceType = consumerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRedisMQConsumer<>))!;
            var queueConsumerAttribute = consumerType.GetCustomAttributes(typeof(RedisMQConsumerAttribute), false).FirstOrDefault() as RedisMQConsumerAttribute;
            if (queueConsumerAttribute == null)
                throw new RedisMQException($"{consumerType.Name} doesn't have a `RedisMQConsumerAttribute`!");
            if (string.IsNullOrEmpty(queueConsumerAttribute.QueueName))
                throw new RedisMQException($"{consumerType.Name}'s `RedisMQConsumerAttribute` queue name is null or empty!");

            var queueName = $"{queueConfig.RedisPrefix}{queueConsumerAttribute.QueueName}";
            var groupName = $"{queueConsumerAttribute.QueueName}_Group";

            if (_cacheList.Any(x => x.QueueName == queueName))
                throw new RedisMQException($"Cannot add queue `{queueName}` repeatedly!");

            _cacheList.Add(new ConsumerExecutorDescriptor
            {
                ConsumerTypeInfo = consumerType.GetTypeInfo(),
                MessageTypeInfo = interfaceType.GetGenericArguments()[0].GetTypeInfo(),
                QueueName = queueName,
                GroupName = groupName,
                IsDelayQueueConsumer = queueConsumerAttribute.IsDelayQueueConsumer,
                PendingTimeOut = queueConsumerAttribute.PendingTimeOut,
                RedisMQConsumerAttribute = queueConsumerAttribute
            });
        }
    }

    public IEnumerable<ConsumerExecutorDescriptor> GetConsumerExecutorDescriptors()
    {
        return _cacheList;
    }
}