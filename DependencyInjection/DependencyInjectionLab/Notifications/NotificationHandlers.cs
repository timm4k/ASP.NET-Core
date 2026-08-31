namespace DependencyInjectionLab.Notifications;

internal interface INotificationHandler
{
    void Send(string message);
}

internal sealed class SmsHandler : INotificationHandler
{
    public void Send(string message)
    {
        Console.WriteLine($"SMS: {message}");
    }
}

internal sealed class PushNotificationHandler : INotificationHandler
{
    public void Send(string message)
    {
        Console.WriteLine($"Push notification: {message}");
    }
}
