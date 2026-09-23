using Microsoft.AspNetCore.Http.Headers;
using ResponseCaching.Contracts;
using ResponseCaching.Services;

namespace ResponseCaching.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        RouteGroupBuilder posts = app.MapGroup("/api/posts");
        posts.MapGet("/{id:int}", GetPostAsync);
        posts.MapPut("/{id:int}", UpdatePostAsync);
        posts.MapPost("/{id:int}/reset-cache", ResetCache);
    }

    private static async Task<IResult> GetPostAsync(
        int id,
        HttpRequest request,
        HttpResponse response,
        PostCacheService posts,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        PostLookupResult? result = await posts.GetAsync(id, cancellationToken);
        if (result is null)
        {
            return Results.NotFound();
        }

        ResponseHeaders responseHeaders = response.GetTypedHeaders();
        responseHeaders.CacheControl = new()
        {
            Private = true,
            NoCache = true,
            MustRevalidate = true
        };
        responseHeaders.LastModified = result.Post.UpdatedAt;
        response.Headers["X-Data-Source"] = result.Source;

        DateTimeOffset? clientVersion = request.GetTypedHeaders().IfModifiedSince;
        if (clientVersion >= result.Post.UpdatedAt)
        {
            loggerFactory.CreateLogger("ConditionalRequest").LogInformation(
                "HTTP 304 for post {PostId} Browser copy is still current",
                id);
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        return Results.Ok(result.Post);
    }

    private static async Task<IResult> UpdatePostAsync(
        int id,
        UpdatePostRequest request,
        PostCacheService posts,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        PostResponse? post = await posts.UpdateAsync(id, request, cancellationToken);
        return post is null ? Results.NotFound() : Results.Ok(post);
    }

    private static IResult ResetCache(int id, PostCacheService posts)
    {
        posts.Remove(id);
        return Results.NoContent();
    }

    private static Dictionary<string, string[]> Validate(UpdatePostRequest request)
    {
        Dictionary<string, string[]> errors = [];
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 3 or > 120)
        {
            errors["title"] = ["Title must contain 3 to 120 characters"];
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length is < 10 or > 2000)
        {
            errors["content"] = ["Content must contain 10 to 2000 characters"];
        }

        return errors;
    }
}
