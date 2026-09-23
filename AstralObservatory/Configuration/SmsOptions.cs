namespace AstralObservatory.Configuration;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";
    public const string DevelopmentProvider = "Development";
    public const string TwilioProvider = "Twilio";

    public required string Provider { get; init; }
    public string? AccountSid { get; init; }
    public string? AuthToken { get; init; }
    public string? FromNumber { get; init; }

    public bool IsDevelopment => Provider.Equals(DevelopmentProvider, StringComparison.OrdinalIgnoreCase);
    public bool IsTwilio => Provider.Equals(TwilioProvider, StringComparison.OrdinalIgnoreCase);

    public bool IsValid() => IsDevelopment ||
        IsTwilio
        && !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(FromNumber)
        && FromNumber.StartsWith('+');
}
