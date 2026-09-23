using Microsoft.EntityFrameworkCore;
using ResponseCaching.Models;

namespace ResponseCaching.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.Posts.AnyAsync())
        {
            return;
        }

        db.Posts.Add(new Post
        {
            Title = "Why conditional requests matter",
            Content = "A cached representation saves database work while HTTP validators keep the browser synchronized with the latest version",
            UpdatedAt = TruncateToSecond(DateTimeOffset.UtcNow)
        });
        await db.SaveChangesAsync();
    }

    private static DateTimeOffset TruncateToSecond(DateTimeOffset value) =>
        DateTimeOffset.FromUnixTimeSeconds(value.ToUnixTimeSeconds());
}
