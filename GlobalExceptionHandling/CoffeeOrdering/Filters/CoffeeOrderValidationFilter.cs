using CoffeeOrdering.Contracts;
using CoffeeOrdering.Services;

namespace CoffeeOrdering.Filters;

public sealed class CoffeeOrderValidationFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        CoffeeOrderRequest? request = context.GetArgument<CoffeeOrderRequest>(0);
        string drink = request?.Drink?.Trim().ToLowerInvariant() ?? string.Empty;

        if (request is null || !CoffeeMenu.IsSupported(drink))
        {
            return ValueTask.FromResult<object?>(Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["drink"] = [$"Choose one of: {string.Join(", ", CoffeeMenu.DrinkNames)}"]
                },
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid coffee order",
                type: "/problems/invalid-drink-type",
                extensions: new Dictionary<string, object?>
                {
                    ["reason"] = "INVALID_DRINK_TYPE",
                    ["localized_message"] = "Choose a drink from the current menu",
                    ["developer_message"] = "The drink field must contain a supported menu identifier"
                }));
        }

        context.Arguments[0] = request with { Drink = drink };
        return next(context);
    }
}
