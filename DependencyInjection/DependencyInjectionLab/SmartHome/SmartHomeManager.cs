namespace DependencyInjectionLab.SmartHome;

internal sealed class SmartHomeManager
{
    private readonly IThermostat _thermostat;
    private readonly ILightController _lightController;
    private readonly ISecuritySystem _securitySystem;

    public SmartHomeManager(
        IThermostat thermostat,
        ILightController lightController,
        ISecuritySystem securitySystem)
    {
        _thermostat = thermostat;
        _lightController = lightController;
        _securitySystem = securitySystem;
    }

    public void ActivateEveningMode()
    {
        _lightController.TurnOff();
        _thermostat.SetTemperature(22);
        _securitySystem.Arm();
    }
}
