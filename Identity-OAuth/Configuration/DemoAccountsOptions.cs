using System.Net.Mail;

namespace IdentityOAuth.Configuration;

public sealed class DemoAccountsOptions
{
    public const string SectionName = "DemoAccounts";

    public List<DemoAccount> Accounts { get; init; } = [];

    public bool IsValid() =>
        Accounts.Count > 0
        && Accounts.All(account => account.IsValid())
        && Accounts.Select(account => account.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == Accounts.Count
        && Accounts.Select(account => account.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == Accounts.Count;
}

public sealed class DemoAccount
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Email { get; init; }
    public required string Description { get; init; }

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Key)
        && Key.Length <= 32
        && Key.All(character => char.IsAsciiLetterOrDigit(character) || character == '-')
        && !string.IsNullOrWhiteSpace(Label)
        && MailAddress.TryCreate(Email, out _)
        && !string.IsNullOrWhiteSpace(Description);
}
