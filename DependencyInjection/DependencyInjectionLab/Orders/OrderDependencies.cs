namespace DependencyInjectionLab.Orders;

internal interface IEmailSender
{
    void SendOrderConfirmation(string orderNumber);
}

internal interface IInventoryCheck
{
    bool IsAvailable(string productCode, int quantity);
}

internal sealed class SmtpEmailSender : IEmailSender
{
    public void SendOrderConfirmation(string orderNumber)
    {
        Console.WriteLine($"Confirmation sent for order {orderNumber}");
    }
}

internal sealed class LocalInventory : IInventoryCheck
{
    public bool IsAvailable(string productCode, int quantity)
    {
        Console.WriteLine($"Inventory checked for {quantity} unit(s) of {productCode}");
        return quantity > 0;
    }
}
