namespace IdentityOAuth.Configuration;

internal static class ConfigurationPath
{
    public static bool IsLocal(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.StartsWith('/')
        && !path.StartsWith("//", StringComparison.Ordinal);
}
