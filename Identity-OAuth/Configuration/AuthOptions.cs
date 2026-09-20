namespace IdentityOAuth.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public required string LoginPath { get; init; }
    public required string AccessDeniedPath { get; init; }
    public TimeSpan CookieLifetime { get; init; }
    public TimeSpan ExternalCookieLifetime { get; init; }
    public required string AdminDomain { get; init; }
    public bool RequireUniqueEmail { get; init; }
    public int LockoutMaxFailedAttempts { get; init; }
    public TimeSpan LockoutDuration { get; init; }
    public required string DefaultRedirectPath { get; init; }

    public bool IsValid() =>
        CookieLifetime == TimeSpan.FromHours(2)
        && ExternalCookieLifetime > TimeSpan.Zero
        && ConfigurationPath.IsLocal(LoginPath)
        && ConfigurationPath.IsLocal(AccessDeniedPath)
        && ConfigurationPath.IsLocal(DefaultRedirectPath)
        && !string.IsNullOrWhiteSpace(AdminDomain)
        && AdminDomain.StartsWith('@')
        && AdminDomain.Length > 1
        && LockoutMaxFailedAttempts > 0
        && LockoutDuration > TimeSpan.Zero;
}
