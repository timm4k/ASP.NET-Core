namespace IdentityOAuth.Configuration;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public required string CallbackPath { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsValid =>
        ConfigurationPath.IsLocal(CallbackPath)
        && (IsConfigured
            || (string.IsNullOrWhiteSpace(ClientId) && string.IsNullOrWhiteSpace(ClientSecret)));
}
