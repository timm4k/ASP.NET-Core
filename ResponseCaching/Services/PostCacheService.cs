using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResponseCaching.Configuration;
using ResponseCaching.Contracts;
using ResponseCaching.Data;

namespace ResponseCaching.Services;

public sealed record PostLookupResult(PostResponse Post, string Source);

public sealed class PostCacheService(
    AppDbContext db,
    IMemoryCache cache,
    IOptions<CachingOptions> options,
    ILogger<PostCacheService> logger)
{
    private const string DatabaseSource = "database";
    private const string CacheSource = "memory-cache";

    public async Task<PostLookupResult?> GetAsync(int id, CancellationToken cancellationToken)
    {
        string key = GetKey(id);
        if (cache.TryGetValue(key, out PostResponse? cachedPost) && cachedPost is not null)
        {
            logger.LogInformation(
                "CACHE HIT for post {PostId} No database query was executed",
                id);
            return new PostLookupResult(cachedPost, CacheSource);
        }

        logger.LogInformation(
            "CACHE MISS for post {PostId} Querying the database",
            id);
        PostResponse? post = await db.Posts
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new PostResponse(item.Id, item.Title, item.Content, item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        logger.LogInformation(
            "DATABASE QUERY completed for post {PostId}",
            id);

        if (post is null)
        {
            return null;
        }

        Set(key, post);
        return new PostLookupResult(post, DatabaseSource);
    }

    public async Task<PostResponse?> UpdateAsync(
        int id,
        UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DATABASE UPDATE starts for post {PostId}",
            id);
        Models.Post? post = await db.Posts.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (post is null)
        {
            return null;
        }

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        post.UpdatedAt = GetNextTimestamp(post.UpdatedAt);
        await db.SaveChangesAsync(cancellationToken);

        PostResponse response = new(post.Id, post.Title, post.Content, post.UpdatedAt);
        Set(GetKey(id), response);
        logger.LogInformation(
            "DATABASE UPDATE completed for post {PostId} Cache refreshed at {UpdatedAt}",
            id,
            post.UpdatedAt);
        return response;
    }

    public void Remove(int id)
    {
        cache.Remove(GetKey(id));
        logger.LogInformation(
            "CACHE RESET for post {PostId} The next request will query the database",
            id);
    }

    private void Set(string key, PostResponse post) =>
        cache.Set(key, post, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.Value.PostLifetime
        });

    private static string GetKey(int id) => $"post:{id}";

    private static DateTimeOffset GetNextTimestamp(DateTimeOffset previous)
    {
        DateTimeOffset current = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        return current > previous ? current : previous.AddSeconds(1);
    }
}
