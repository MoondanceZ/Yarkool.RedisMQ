using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ.Internal;

/// <summary>
/// Cap options extension
/// </summary>
public interface IRedisMQOptionsExtension
{
    /// <summary>
    /// Registered child service.
    /// </summary>
    /// <param name="services">add service to the <see cref="IServiceCollection" /></param>
    void AddServices(IServiceCollection services);
}