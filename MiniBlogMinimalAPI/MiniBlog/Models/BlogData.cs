namespace MiniBlog.Models;

public sealed class BlogData
{
    public List<BlogPost> Posts { get; init; } = [];
    public List<Category> Categories { get; init; } = [];
}
