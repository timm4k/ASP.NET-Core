using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.Payments;

internal static class PaymentDemonstration
{
    public static void Run()
    {
        Console.WriteLine("2.2 Replaceable payment processor");

        ServiceCollection services = new();
        services.AddTransient<IPaymentProcessor, MastercardProcessor>();
        services.AddTransient<CheckoutService>();

        using ServiceProvider provider = services.BuildServiceProvider();
        CheckoutService checkoutService = provider.GetRequiredService<CheckoutService>();
        checkoutService.Checkout(149.90m);
        Console.WriteLine();
    }
}
