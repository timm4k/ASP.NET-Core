using DependencyInjectionLab.Logging;
using DependencyInjectionLab.Notifications;
using DependencyInjectionLab.Orders;
using DependencyInjectionLab.Payments;
using DependencyInjectionLab.SmartHome;
using DependencyInjectionLab.Traffic;

namespace DependencyInjectionLab;

internal static class DependencyInjectionDemonstration
{
    public static void Run()
    {
        TrafficLifetimeDemonstration.Run();
        OrderResolutionDemonstration.Run();
        NotificationDemonstration.Run();
        SmartHomeDemonstration.Run();
        PaymentDemonstration.Run();
        LoggerFactoryDemonstration.Run();
    }
}
