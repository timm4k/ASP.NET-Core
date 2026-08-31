using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.Traffic;

internal static class TrafficLifetimeDemonstration
{
    public static void Run()
    {
        Console.WriteLine("1.1 Traffic counter");
        ShowTransientLifetime();
        ShowSingletonLifetime();
        Console.WriteLine();
    }

    private static void ShowTransientLifetime()
    {
        ServiceCollection services = new();
        services.AddTransient<ITrafficCounter, TrafficCounter>();

        using ServiceProvider provider = services.BuildServiceProvider();
        ITrafficCounter first = ResolveCounter(provider);
        ITrafficCounter second = ResolveCounter(provider);

        Console.WriteLine($"Transient counts: {first.Count}, {second.Count}");
        Console.WriteLine($"Transient same instance: {ReferenceEquals(first, second)}");
    }

    private static void ShowSingletonLifetime()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITrafficCounter, TrafficCounter>();

        using ServiceProvider provider = services.BuildServiceProvider();
        ITrafficCounter first = ResolveCounter(provider);
        Console.WriteLine($"Singleton first count: {first.Count}");

        first.RecordTraffic();
        ITrafficCounter second = ResolveCounter(provider);
        Console.WriteLine($"Singleton second count: {second.Count}");
        Console.WriteLine($"Singleton same instance: {ReferenceEquals(first, second)}");
    }

    private static ITrafficCounter ResolveCounter(ServiceProvider provider)
    {
        return provider.GetService<ITrafficCounter>()
            ?? throw new InvalidOperationException("Traffic counter is not registered");
    }
}
