namespace Yarkool.Redis.Queue;

public interface ISubscriber
{
    /// <summary>
    /// Subscribe
    /// </summary>
    /// <returns></returns>
    Task SubscribeAsync();
}