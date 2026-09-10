namespace CoffeeOrdering.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SecretKey { get; init; }
    public int AccessTokenMinutes { get; init; }
    public int RefreshTokenDays { get; init; }
}
