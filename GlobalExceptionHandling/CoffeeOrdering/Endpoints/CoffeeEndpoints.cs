using CoffeeOrdering.Contracts;
using CoffeeOrdering.Filters;
using CoffeeOrdering.Services;

namespace CoffeeOrdering.Endpoints;

public static class CoffeeEndpoints
{
    public static IEndpointRouteBuilder MapCoffeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/menu", () => Results.Ok(CoffeeMenu.GetCatalog()));

        endpoints.MapPost("/order", async (
            CoffeeOrderRequest request,
            CoffeeMachine coffeeMachine,
            CancellationToken cancellationToken) =>
        {
            CoffeeOrderResponse order = await coffeeMachine.PrepareAsync(request.Drink!, cancellationToken);
            return Results.Ok(order);
        }).AddEndpointFilter<CoffeeOrderValidationFilter>();

        return endpoints;
    }
}
