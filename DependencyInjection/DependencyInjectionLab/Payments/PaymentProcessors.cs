namespace DependencyInjectionLab.Payments;

internal interface IPaymentProcessor
{
    void Process(decimal amount);
}

internal sealed class VisaProcessor : IPaymentProcessor
{
    public void Process(decimal amount)
    {
        Console.WriteLine($"Visa charged {amount:F2}");
    }
}

internal sealed class MastercardProcessor : IPaymentProcessor
{
    public void Process(decimal amount)
    {
        Console.WriteLine($"Mastercard charged {amount:F2}");
    }
}
