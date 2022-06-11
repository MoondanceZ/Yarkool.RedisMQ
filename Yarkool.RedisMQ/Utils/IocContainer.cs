using Microsoft.Extensions.DependencyInjection;

namespace Yarkool.RedisMQ;

public class IocContainer
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static TService? Resolve<TService>()
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService<TService>();
    }

    public static object? Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService(type);
    }
}