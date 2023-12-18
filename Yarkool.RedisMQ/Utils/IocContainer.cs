using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ;

public class IocContainer
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static TService? GetService<TService>()
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService<TService>();
    }

    public static IEnumerable<TService> GetServices<TService>()
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetServices<TService>();
    }

    public static object? GetService(Type type)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService(type);
    }

    public static IEnumerable<object?> GetServices(Type type)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetServices(type);
    }
}