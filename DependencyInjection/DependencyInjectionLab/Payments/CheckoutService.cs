namespace DependencyInjectionLab.Payments;

internal sealed class CheckoutService
{
    private readonly IPaymentProcessor _paymentProcessor;

    public CheckoutService(IPaymentProcessor paymentProcessor)
    {
        _paymentProcessor = paymentProcessor;
    }

    public void Checkout(decimal total)
    {
        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Checkout total must be positive");
        }

        _paymentProcessor.Process(total);
        Console.WriteLine("Checkout completed");
    }
}
