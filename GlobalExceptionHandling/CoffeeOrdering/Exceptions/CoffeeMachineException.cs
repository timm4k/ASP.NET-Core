namespace CoffeeOrdering.Exceptions;

public sealed class CoffeeMachineException(
    string message,
    string reason,
    int retryAfterSeconds) : Exception(message)
{
    public string Reason { get; } = reason;
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
