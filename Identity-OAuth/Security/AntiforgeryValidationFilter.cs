using Microsoft.AspNetCore.Antiforgery;

namespace IdentityOAuth.Security;

internal sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new
            {
                error = "The security token is missing or invalid"
            });
        }

        return await next(context);
    }
}
