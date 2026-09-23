namespace AstralObservatory.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public TimeSpan AccessTokenLifetime { get; init; }
    public TimeSpan PendingTokenLifetime { get; init; }

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && AccessTokenLifetime > TimeSpan.Zero
        && PendingTokenLifetime > TimeSpan.Zero
        && PendingTokenLifetime < AccessTokenLifetime;
}
