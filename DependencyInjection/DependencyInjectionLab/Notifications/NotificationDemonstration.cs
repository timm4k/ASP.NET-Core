using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.Notifications;

internal static class NotificationDemonstration
{
    public static void Run()
    {
        Console.WriteLine("1.3 Multiple notification handlers");

        ServiceCollection services = new();
        services.AddTransient<INotificationHandler, SmsHandler>();
        services.AddTransient<INotificationHandler, PushNotificationHandler>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<INotificationHandler> handlers = provider.GetServices<INotificationHandler>();

        foreach (INotificationHandler handler in handlers)
        {
            handler.Send("System updated");
        }

        Console.WriteLine();
    }
}
