namespace Yarkool.RedisMQ.Internal;

/// <inheritdoc />
/// <summary>
/// A process thread abstract of message process.
/// </summary>
public interface IProcessingServer : IDisposable
{
    Task Start(CancellationToken stoppingToken);
}