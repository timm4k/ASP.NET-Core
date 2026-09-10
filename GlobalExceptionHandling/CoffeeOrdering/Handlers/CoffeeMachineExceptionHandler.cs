using CoffeeOrdering.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CoffeeOrdering.Handlers;

public sealed class CoffeeMachineExceptionHandler(
    ILogger<CoffeeMachineExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not CoffeeMachineException coffeeException)
        {
            return false;
        }

        logger.LogWarning(
            coffeeException,
            "Coffee machine rejected order {TraceIdentifier} with reason {Reason}",
            httpContext.TraceIdentifier,
            coffeeException.Reason);

        httpContext.Response.Headers.RetryAfter = coffeeException.RetryAfterSeconds.ToString();
        await Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Coffee machine unavailable",
            detail: "The machine cannot prepare this order right now",
            type: "/problems/coffee-machine-unavailable",
            extensions: new Dictionary<string, object?>
            {
                ["reason"] = coffeeException.Reason,
                ["localized_message"] = "The water tank is empty. The machine is refilling",
                ["developer_message"] = coffeeException.Message,
                ["retry_after_seconds"] = coffeeException.RetryAfterSeconds
            }).ExecuteAsync(httpContext);

        return true;
    }
}
