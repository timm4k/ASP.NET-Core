using CoffeeOrdering.Configuration;
using CoffeeOrdering.Contracts;
using CoffeeOrdering.Http;
using CoffeeOrdering.Services;

namespace CoffeeOrdering.Endpoints;

public static class NoteEndpoints
{
    private const int MaximumTitleLength = 80;
    private const int MaximumContentLength = 1000;

    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder notes = endpoints.MapGroup("/notes")
            .RequireAuthorization(AuthorizationPolicies.NotesOwner);

        notes.MapGet("/", (HttpContext context, NoteStore store) =>
            Results.Ok(store.GetAll(context.User.GetUserId())));

        notes.MapGet("/{id:guid}", (Guid id, HttpContext context, NoteStore store) =>
            store.Get(id, context.User.GetUserId()) is { } note ? Results.Ok(note) : NotFound());

        notes.MapPost("/", (CreateNoteRequest request, HttpContext context, NoteStore store) =>
        {
            if (Validate(request.Title, request.Content) is { } validation)
            {
                return validation;
            }

            NoteResponse note = store.Create(context.User.GetUserId(), request.Title!, request.Content!);
            return Results.Created($"/notes/{note.Id}", note);
        });

        notes.MapPut("/{id:guid}", (Guid id, UpdateNoteRequest request, HttpContext context, NoteStore store) =>
        {
            if (Validate(request.Title, request.Content) is { } validation)
            {
                return validation;
            }

            NoteResponse? note = store.Update(id, context.User.GetUserId(), request.Title!, request.Content!);
            return note is null ? NotFound() : Results.Ok(note);
        });

        notes.MapDelete("/{id:guid}", (Guid id, HttpContext context, NoteStore store) =>
            store.Delete(id, context.User.GetUserId()) ? Results.NoContent() : NotFound());

        return endpoints;
    }

    private static IResult? Validate(string? title, string? content)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > MaximumTitleLength)
        {
            errors["title"] = [$"Title must contain 1 to {MaximumTitleLength} characters"];
        }

        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > MaximumContentLength)
        {
            errors["content"] = [$"Content must contain 1 to {MaximumContentLength} characters"];
        }

        return errors.Count == 0
            ? null
            : ProblemResults.Validation(
                errors,
                "INVALID_NOTE",
                "Check the note title and content",
                "Note input did not satisfy length constraints");
    }

    private static IResult NotFound() => ProblemResults.Problem(
        StatusCodes.Status404NotFound,
        "Note was not found",
        "The note does not exist or belongs to another user",
        "NOTE_NOT_FOUND",
        "This note is not available",
        "Owner-scoped note lookup returned no result");
}
