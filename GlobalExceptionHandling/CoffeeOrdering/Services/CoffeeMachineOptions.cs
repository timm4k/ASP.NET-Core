namespace CoffeeOrdering.Services;

public sealed class CoffeeMachineOptions
{
    public const string SectionName = "CoffeeMachine";

    public int WaterCapacityMilliliters { get; init; }
    public int AutomaticRefillSeconds { get; init; }
    public int PreparationMilliseconds { get; init; }
    public int TimeoutEveryOrders { get; init; }
}
