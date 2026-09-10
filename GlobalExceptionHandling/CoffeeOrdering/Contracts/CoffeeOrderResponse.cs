namespace CoffeeOrdering.Contracts;

public sealed record CoffeeOrderResponse(
    Guid OrderId,
    string Drink,
    string DisplayName,
    int PreparationMilliseconds,
    int WaterCapacityMilliliters,
    int RemainingWaterMilliliters,
    DateTimeOffset ReadyAt);
