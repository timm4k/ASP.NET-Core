namespace Books.Api.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/books");

        group.MapGet("/", async (BookService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        group.MapGet("/{id:int}", async (int id, BookService service, CancellationToken cancellationToken) =>
        {
            var book = await service.GetByIdAsync(id, cancellationToken);
            return book is null ? Results.NotFound() : Results.Ok(book);
        });

        group.MapPost("/", async (SaveBookRequest request, BookService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var book = await service.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/books/{book.Id}", book);
            }
            catch (BookValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        });

        group.MapPut("/{id:int}", async (int id, SaveBookRequest request, BookService service, CancellationToken cancellationToken) =>
        {
            try
            {
                return await service.UpdateAsync(id, request, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }
            catch (BookValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        });

        group.MapDelete("/{id:int}", async (int id, BookService service, CancellationToken cancellationToken) =>
            await service.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

        return endpoints;
    }
}
