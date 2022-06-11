namespace Yarkool.RedisMQ;

public interface ISubscriber
{
    /// <summary>
    /// Subscribe
    /// </summary>
    /// <returns></returns>
    Task SubscribeAsync();
}