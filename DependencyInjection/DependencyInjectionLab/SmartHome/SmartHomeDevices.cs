namespace DependencyInjectionLab.SmartHome;

internal interface IThermostat
{
    void SetTemperature(int degreesCelsius);
}

internal interface ILightController
{
    void TurnOff();
}

internal interface ISecuritySystem
{
    void Arm();
}

internal sealed class Thermostat : IThermostat
{
    public void SetTemperature(int degreesCelsius)
    {
        Console.WriteLine($"Thermostat set to {degreesCelsius} C");
    }
}

internal sealed class LightController : ILightController
{
    public void TurnOff()
    {
        Console.WriteLine("Lights turned off");
    }
}

internal sealed class SecuritySystem : ISecuritySystem
{
    public void Arm()
    {
        Console.WriteLine("Security system armed");
    }
}
