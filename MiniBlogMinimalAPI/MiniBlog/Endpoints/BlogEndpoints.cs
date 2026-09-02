using MiniBlog.Models;
using MiniBlog.Services;

namespace MiniBlog.Endpoints;

public static class BlogEndpoints
{
    private const string PostNotFoundMessage = "Post was not found";
    private const string CategoryNotFoundMessage = "Category was not found";
    public static IEndpointRouteBuilder MapBlogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/posts", async (BlogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetPostsAsync(cancellationToken: cancellationToken)));

        endpoints.MapGet("/posts/q", async (
            string? search,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["search"] = ["Enter a search term"]
                });
            }

            return Results.Ok(await repository.GetPostsAsync(search: search, cancellationToken: cancellationToken));
        });

        endpoints.MapGet("/posts/category/{categoryName}", async (
            string categoryName,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CategoryResponse? category = (await repository.GetCategoriesAsync(cancellationToken))
                .FirstOrDefault(item => string.Equals(
                    item.Slug,
                    SlugGenerator.Create(categoryName),
                    StringComparison.OrdinalIgnoreCase));
            if (category is null)
            {
                return Results.NotFound(new { message = CategoryNotFoundMessage });
            }

            return Results.Ok(await repository.GetPostsAsync(
                categorySlug: category.Slug,
                cancellationToken: cancellationToken));
        });

        endpoints.MapGet("/posts/mine", async (
            HttpRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<IReadOnlyList<PostResponse>> result = await repository.GetOwnedPostsAsync(
                request.Headers["X-Author-Key"].FirstOrDefault(),
                cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.ValidationProblem(result.Errors);
        });
        endpoints.MapGet("/posts/{id:int}", async (
            int id,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            PostResponse? post = await repository.GetPostByIdAsync(id, cancellationToken);
            return post is null
                ? Results.NotFound(new { message = PostNotFoundMessage })
                : Results.Ok(post);
        });

        endpoints.MapGet("/posts/{slug}", async (
            string slug,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            PostResponse? post = await repository.GetPostBySlugAsync(slug, cancellationToken);
            return post is null
                ? Results.NotFound(new { message = PostNotFoundMessage })
                : Results.Ok(post);
        });

        endpoints.MapGet("/categories", async (
            BlogRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetCategoriesAsync(cancellationToken)));

        endpoints.MapPost("/images/upload", async (
            HttpRequest request,
            ImageStorage imageStorage,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = ["Send an image using multipart form data"]
                });
            }

            IFormCollection form = await request.ReadFormAsync(cancellationToken);
            CommandResult<string> result = await imageStorage.SaveAsync(
                form.Files.GetFile("image"),
                cancellationToken);
            return result.IsSuccess
                ? Results.Ok(new { imageUrl = result.Value })
                : Results.ValidationProblem(result.Errors);
        }).DisableAntiforgery();
        endpoints.MapPost("/posts/add", async (
            PostCreateRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<PostResponse> result = await repository.AddPostAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created(result.Value!.Url, result.Value)
                : Results.ValidationProblem(result.Errors);
        });

        endpoints.MapPost("/posts/edit", async (
            PostEditRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<PostResponse> result = await repository.EditPostAsync(request, cancellationToken);
            return ToUpdateResult(result, PostNotFoundMessage);
        });

        endpoints.MapPut("/posts/active", async (
            PostActivationRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<PostResponse> result = await repository.SetPostActiveAsync(
                request.Id,
                request.AuthorKey,
                request.IsActive,
                cancellationToken);
            return ToUpdateResult(result, PostNotFoundMessage);
        });
        endpoints.MapPost("/categories/add", async (
            CategoryCreateRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<CategoryResponse> result = await repository.AddCategoryAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/categories#{result.Value!.Slug}", result.Value)
                : Results.ValidationProblem(result.Errors);
        });

        endpoints.MapPost("/categories/edit", async (
            CategoryEditRequest request,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
        {
            CommandResult<CategoryResponse> result = await repository.EditCategoryAsync(request, cancellationToken);
            return ToUpdateResult(result, CategoryNotFoundMessage);
        });

        endpoints.MapDelete("/posts/delete/{id:int}", async (
            int id,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
            await repository.DeletePostAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound(new { message = PostNotFoundMessage }));

        endpoints.MapDelete("/categories/delete/{id:int}", async (
            int id,
            BlogRepository repository,
            CancellationToken cancellationToken) =>
            await repository.DeleteCategoryAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound(new { message = CategoryNotFoundMessage }));

        return endpoints;
    }

    private static IResult ToUpdateResult<T>(CommandResult<T> result, string notFoundMessage)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Errors.ContainsKey("id")
            ? Results.NotFound(new { message = notFoundMessage })
            : Results.ValidationProblem(result.Errors);
    }
}
