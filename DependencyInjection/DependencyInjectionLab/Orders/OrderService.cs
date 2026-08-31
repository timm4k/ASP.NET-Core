namespace DependencyInjectionLab.Orders;

internal sealed class OrderService
{
    private readonly IEmailSender _emailSender;
    private readonly IInventoryCheck _inventoryCheck;

    public OrderService(IEmailSender emailSender, IInventoryCheck inventoryCheck)
    {
        _emailSender = emailSender;
        _inventoryCheck = inventoryCheck;
    }

    public void PlaceOrder(string orderNumber, string productCode, int quantity)
    {
        if (!_inventoryCheck.IsAvailable(productCode, quantity))
        {
            Console.WriteLine($"Order {orderNumber} cannot be placed");
            return;
        }

        Console.WriteLine($"Order {orderNumber} placed");
        _emailSender.SendOrderConfirmation(orderNumber);
    }
}
