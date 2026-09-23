using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AstralObservatory.Configuration;
using AstralObservatory.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AstralObservatory.Services;

public sealed class JwtSigningKey
{
    public SymmetricSecurityKey Key { get; } = new(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
}

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken CreatePendingToken(ApplicationUser user);
    IssuedToken CreateAccessToken(ApplicationUser user);
}

public sealed class JwtTokenService(
    JwtSigningKey signingKey,
    IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public IssuedToken CreatePendingToken(ApplicationUser user) =>
        Create(user, "2fa_pending", _options.PendingTokenLifetime);

    public IssuedToken CreateAccessToken(ApplicationUser user) =>
        Create(user, "access", _options.AccessTokenLifetime);

    private IssuedToken Create(ApplicationUser user, string tokenType, TimeSpan lifetime)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("token_type", tokenType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        ];

        SigningCredentials credentials = new(signingKey.Key, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            _options.Issuer,
            _options.Audience,
            claims,
            DateTime.UtcNow,
            expiresAt.UtcDateTime,
            credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
