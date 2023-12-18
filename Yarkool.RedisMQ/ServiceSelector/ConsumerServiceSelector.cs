using System.Reflection;

namespace Yarkool.RedisMQ;

public class ConsumerServiceSelector
{
    private readonly QueueConfig _queueConfig;
    private readonly List<ConsumerExecutorDescriptor> _cacheList;

    public ConsumerServiceSelector(QueueConfig queueConfig)
    {
        _queueConfig = queueConfig;
        _cacheList = new List<ConsumerExecutorDescriptor>();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var basePublisherGenericType = typeof(BaseConsumer<>);
        var subscriberTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == basePublisherGenericType)
            .ToList();

        foreach (var subscriberType in subscriberTypes)
        {
            var queueSubscriberAttribute = subscriberType.GetCustomAttributes(typeof(QueueConsumerAttribute), false).FirstOrDefault() as QueueConsumerAttribute;
            ArgumentNullException.ThrowIfNull(queueSubscriberAttribute, nameof(QueueConsumerAttribute));

            var queueName = $"{_queueConfig.RedisPrefix}{queueSubscriberAttribute.QueueName}";
            var groupName = $"{queueSubscriberAttribute.QueueName}_Group";
            
            _cacheList.Add(new ConsumerExecutorDescriptor
            {
                TypeInfo = subscriberType.GetTypeInfo(),
                QueueName = queueName,
                GroupName = groupName,
                QueueConsumerAttribute = queueSubscriberAttribute
            });
        }
    }

    public IEnumerable<ConsumerExecutorDescriptor> GetConsumerExecutorDescriptors()
    {
        return _cacheList;
    }
}