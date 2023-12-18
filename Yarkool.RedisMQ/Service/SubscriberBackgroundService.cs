using System.Text;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yarkool.RedisMQ;

public class SubscriberBackgroundService : BackgroundService
{
    private readonly ConsumerServiceSelector _consumerServiceSelector;
    private readonly RedisClient _redisClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriberBackgroundService> _logger;

    public SubscriberBackgroundService(ConsumerServiceSelector consumerServiceSelector, QueueConfig queueConfig, RedisClient redisClient, ErrorPublisher errorPublisher, IServiceProvider serviceProvider, ILogger<SubscriberBackgroundService> logger)
    {
        _consumerServiceSelector = consumerServiceSelector;
        _redisClient = redisClient;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var i = 0; i < _consumerServiceSelector.GetConsumerExecutorDescriptors().Count(); i++)
        {
            var subscriberIndex = i + 1;
            var consumerExecutorDescriptor = _consumerServiceSelector.GetConsumerExecutorDescriptors().ElementAt(i);

            var queueName = consumerExecutorDescriptor.QueueName;
            var groupName = consumerExecutorDescriptor.GroupName;
            var subscriberName = $"{consumerExecutorDescriptor.QueueName}_Subscriber";

            _logger?.LogInformation($"{consumerExecutorDescriptor.QueueConsumerAttribute.QueueName} {subscriberName}_{subscriberIndex} subscribing");

            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = default(BaseMessage);
                    var data = await _redisClient.XReadGroupAsync(groupName, $"{subscriberName}_{subscriberIndex}", 100, queueName, ">");
                    if (data != null)
                    {
                        //https://github.com/dotnetcore/CAP/blob/master/src/DotNetCore.CAP/Internal/IConsumerServiceSelector.Default.cs#L21
                        var subscriber = _serviceProvider.CreateScope().ServiceProvider.GetServices(typeof(IConsumer)).First(x => x?.GetType() == subscriberType);
                        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
                        try
                        {
                            message = data.fieldValues.MapToClass<BaseMessage>(Encoding.UTF8);

                            //Execute message
                            await (Task) subscriberType.GetMethod("OnMessageAsync")!.Invoke(subscriber, new[] { message.MessageContent, stoppingToken })!;

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
                                        ErrorMessageContent = message.MessageContent,
                                        ErrorMessageTimestamp = message.CreateTimestamp
                                    };
                                    await _errorPublisher.PublishAsync(errorMessage);

                                    //Execute message
                                    await (Task) subscriberType.GetMethod("OnErrorAsync")!.Invoke(subscriber, new[] { message.MessageContent, stoppingToken })!;
                                }
                            }
                            catch (Exception errorEx)
                            {
                                _logger?.LogError(new RedisMQDataException("Handle error exception!", message, errorEx), "Handle error exception!");
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