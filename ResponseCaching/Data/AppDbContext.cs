using Microsoft.EntityFrameworkCore;
using ResponseCaching.Models;

namespace ResponseCaching.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Post>().Property(post => post.Title).HasMaxLength(120);
        builder.Entity<Post>().Property(post => post.Content).HasMaxLength(2000);
    }
}
