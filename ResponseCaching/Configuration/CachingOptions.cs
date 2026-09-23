namespace ResponseCaching.Configuration;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public TimeSpan PostLifetime { get; init; }

    public bool IsValid() => PostLifetime > TimeSpan.Zero;
}
