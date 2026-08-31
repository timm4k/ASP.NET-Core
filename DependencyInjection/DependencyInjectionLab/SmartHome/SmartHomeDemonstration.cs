using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.SmartHome;

internal static class SmartHomeDemonstration
{
    public static void Run()
    {
        Console.WriteLine("2.1 Smart home constructor injection");

        ServiceCollection services = new();
        services.AddTransient<IThermostat, Thermostat>();
        services.AddTransient<ILightController, LightController>();
        services.AddTransient<ISecuritySystem, SecuritySystem>();
        services.AddTransient<SmartHomeManager>();

        using ServiceProvider provider = services.BuildServiceProvider();
        SmartHomeManager manager = provider.GetRequiredService<SmartHomeManager>();
        manager.ActivateEveningMode();
        Console.WriteLine();
    }
}
