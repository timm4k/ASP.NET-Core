using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoffeeOrdering.Configuration;
using CoffeeOrdering.Contracts;
using CoffeeOrdering.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeOrdering.Services;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    RefreshTokenStore refreshTokenStore,
    TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    public TokenPairResponse CreateTokenPair(AppUser user)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        DateTimeOffset refreshExpiresAt = now.AddDays(_options.RefreshTokenDays);
        string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("username", user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.SecretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        refreshTokenStore.Store(refreshToken, new RefreshSession(user.Id, refreshExpiresAt));

        return new TokenPairResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            refreshToken,
            accessExpiresAt,
            refreshExpiresAt);
    }
}
