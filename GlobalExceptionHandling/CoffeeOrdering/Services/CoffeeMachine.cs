using CoffeeOrdering.Contracts;
using CoffeeOrdering.Exceptions;
using CoffeeOrdering.Models;
using Microsoft.Extensions.Options;

namespace CoffeeOrdering.Services;

public sealed class CoffeeMachine
{
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly CoffeeMachineOptions _options;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset? _refillCompletesAt;
    private int _remainingWaterMilliliters;
    private int _orderSequence;

    public CoffeeMachine(IOptions<CoffeeMachineOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _remainingWaterMilliliters = _options.WaterCapacityMilliliters;
    }

    public async Task<CoffeeOrderResponse> PrepareAsync(
        string drink,
        CancellationToken cancellationToken)
    {
        CoffeeRecipe recipe = CoffeeMenu.Get(drink);
        CoffeeOrderResponse order;

        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            CompleteRefillIfReady(now);
            int orderNumber = checked(++_orderSequence);

            if (_options.TimeoutEveryOrders > 0 && orderNumber % _options.TimeoutEveryOrders == 0)
            {
                throw new TimeoutException("The simulated brewing cycle exceeded its time limit");
            }

            if (_remainingWaterMilliliters < recipe.WaterMilliliters)
            {
                _refillCompletesAt ??= now.AddSeconds(_options.AutomaticRefillSeconds);
                int retryAfterSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((_refillCompletesAt.Value - now).TotalSeconds));
                throw new CoffeeMachineException(
                    "The water reservoir does not contain enough water for the selected drink",
                    "OUT_OF_WATER",
                    retryAfterSeconds);
            }

            _remainingWaterMilliliters -= recipe.WaterMilliliters;
            order = new CoffeeOrderResponse(
                Guid.NewGuid(),
                recipe.Name,
                recipe.DisplayName,
                _options.PreparationMilliseconds,
                _options.WaterCapacityMilliliters,
                _remainingWaterMilliliters,
                now.AddMilliseconds(_options.PreparationMilliseconds));
        }
        finally
        {
            _stateGate.Release();
        }

        await Task.Delay(_options.PreparationMilliseconds, cancellationToken);
        return order;
    }

    private void CompleteRefillIfReady(DateTimeOffset now)
    {
        if (_refillCompletesAt is null || now < _refillCompletesAt)
        {
            return;
        }

        _remainingWaterMilliliters = _options.WaterCapacityMilliliters;
        _refillCompletesAt = null;
    }
}
