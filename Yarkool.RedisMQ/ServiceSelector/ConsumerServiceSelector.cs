using System.Reflection;

namespace Yarkool.RedisMQ;

public class ConsumerServiceSelector
{
    private readonly List<ConsumerExecutorDescriptor> _cacheList;

    public ConsumerServiceSelector(QueueConfig queueConfig)
    {
        _cacheList = new List<ConsumerExecutorDescriptor>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var basePublisherGenericType = typeof(BaseConsumer<>);
        var consumerTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType is
            {
                IsGenericType: true
            } && t.BaseType.GetGenericTypeDefinition() == basePublisherGenericType)
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            var baseType = consumerType.BaseType;
            var queueConsumerAttribute = consumerType.GetCustomAttributes(typeof(QueueConsumerAttribute), false).FirstOrDefault() as QueueConsumerAttribute;
            ArgumentNullException.ThrowIfNull(queueConsumerAttribute, nameof(QueueConsumerAttribute));

            var queueName = $"{queueConfig.RedisPrefix}{queueConsumerAttribute.QueueName}";
            var groupName = $"{queueConsumerAttribute.QueueName}_Group";

            _cacheList.Add(new ConsumerExecutorDescriptor
            {
                ConsumerTypeInfo = consumerType.GetTypeInfo(),
                MessageTypeInfo = baseType!.GetGenericArguments()[0].GetTypeInfo(),
                QueueName = queueName,
                GroupName = groupName,
                QueueConsumerAttribute = queueConsumerAttribute
            });
        }
    }

    public IEnumerable<ConsumerExecutorDescriptor> GetConsumerExecutorDescriptors()
    {
        return _cacheList;
    }
}