namespace MiniBlog.Models;

public sealed record Category(
    int Id,
    string Name,
    string Slug);
