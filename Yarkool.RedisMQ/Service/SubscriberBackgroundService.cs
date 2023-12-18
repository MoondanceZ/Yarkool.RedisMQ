using System.Text;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ;

public class SubscriberBackgroundService : BackgroundService
{
    private readonly QueueConfig _queueConfig;
    private readonly RedisClient _redisClient;
    private readonly ISerializer _serializer;
    private readonly ErrorPublisher _errorPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriberBackgroundService> _logger;
    private readonly Dictionary<Type, QueueSubscriberAttribute> _queueSubscriberDic = new Dictionary<Type, QueueSubscriberAttribute>();

    public SubscriberBackgroundService(QueueConfig queueConfig, RedisClient redisClient, ErrorPublisher errorPublisher,IServiceProvider serviceProvider, ILogger<SubscriberBackgroundService> logger)
    {
        _queueConfig = queueConfig ?? throw new ArgumentNullException(nameof(queueConfig));
        _serializer = _queueConfig.Serializer;
        _redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
        _errorPublisher = errorPublisher ?? throw new ArgumentNullException(nameof(errorPublisher));
        _serviceProvider = serviceProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var subscriberServices = _serviceProvider.GetServices<ISubscriber>();
        foreach (var subscriber in subscriberServices)
        {
            var subscriberType = subscriber!.GetType();
            var queueSubscriberAttribute = subscriberType.GetCustomAttributes(typeof(QueueSubscriberAttribute), false).FirstOrDefault() as QueueSubscriberAttribute;
            ArgumentNullException.ThrowIfNull(queueSubscriberAttribute, nameof(QueueSubscriberAttribute));

            _queueSubscriberDic.Add(subscriberType, queueSubscriberAttribute);

            var queueName = $"{_queueConfig.RedisPrefix}{queueSubscriberAttribute.QueueName}";
            var groupName = $"{queueSubscriberAttribute.QueueName}_Group";
            var subscriberName = $"{queueSubscriberAttribute.QueueName}_Subscriber";

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var i = 0; i < _queueSubscriberDic.Count; i++)
        {
            var subscriberIndex = i + 1;

            var queueSubscriberItem = _queueSubscriberDic.ElementAt(i);
            var queueSubscriberAttribute = queueSubscriberItem.Value;

            var queueName = $"{_queueConfig.RedisPrefix}{queueSubscriberAttribute.QueueName}";
            var groupName = $"{queueSubscriberAttribute.QueueName}_Group";
            var subscriberName = $"{queueSubscriberAttribute.QueueName}_Subscriber";

            var actualQueueName = string.IsNullOrEmpty(_queueConfig.RedisPrefix) ? queueName : queueName.Replace(_queueConfig.RedisPrefix, "");
            _logger.LogInformation($"{actualQueueName} {subscriberName}_{subscriberIndex} subscribing");

            await Task.Run(async () =>
            {
                var subscriberType = queueSubscriberItem.Key;
                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = default(BaseMessage);
                    var data = await _redisClient.XReadGroupAsync(groupName, $"{subscriberName}_{subscriberIndex}", 100, queueName, ">");
                    if (data != null)
                    {
                        var subscriber = _serviceProvider.CreateScope().ServiceProvider.GetService(queueSubscriberItem.Key) as ISubscriber;
                        ArgumentNullException.ThrowIfNull(subscriber, queueSubscriberItem.Key.Name);

                        try
                        {
                            message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);

                            //Execute message
                            await subscriber.OnMessageAsync(message.MessageContent, stoppingToken);

                            //ACK
                            await _redisClient.XAckAsync(queueName, groupName, data.id);
                            await _redisClient.XDelAsync(queueName, data.id);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (_queueConfig.UseErrorQueue && message != null)
                                {
                                    var errorMessage = new ErrorMessage
                                    {
                                        SubscriberName = subscriberName,
                                        ExceptionMessage = ex.Message,
                                        StackTrace = ex.StackTrace,
                                        GroupName = groupName,
                                        QueueName = queueName,
                                        MessageContent = message.MessageContent,
                                        ErrorMessageTimestamp = message.CreateTimestamp
                                    };
                                    await _errorPublisher.PublishAsync(errorMessage);

                                    await subscriber.OnErrorAsync(message, stoppingToken);
                                }
                            }
                            catch (Exception errorEx)
                            {
                                _logger.LogError(new RedisMQDataException("Handle error exception!", message, errorEx), "Handle error exception!");
                            }
                        }
                        finally
                        {
                            message = null;
                        }
                    }
                }
            }, stoppingToken);
        }
    }
}