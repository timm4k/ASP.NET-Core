using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.Orders;

internal static class OrderResolutionDemonstration
{
    public static void Run()
    {
        Console.WriteLine("1.2 Complete dependency chain");

        ServiceCollection services = new();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddTransient<IInventoryCheck, LocalInventory>();
        services.AddTransient<OrderService>();

        using ServiceProvider provider = services.BuildServiceProvider();
        OrderService orderService = provider.GetService<OrderService>()
            ?? throw new InvalidOperationException("Order service is not registered");

        orderService.PlaceOrder("ORD-1001", "LAPTOP-STAND", 1);
        Console.WriteLine();
    }
}
