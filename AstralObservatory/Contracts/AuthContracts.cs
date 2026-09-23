namespace AstralObservatory.Contracts;

public sealed record RegisterRequest(string Name, string Email, string PhoneNumber, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record SmsVerificationRequest(string Code);

public sealed record TotpVerificationRequest(string Code);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record LoginResponse(string ChallengeToken, DateTimeOffset ExpiresAt, string[] AvailableMethods);

public sealed record SmsChallengeResponse(
    string Message,
    string DeliveryMode,
    string MaskedDestination,
    string? DevelopmentCode);

public sealed record TotpSetupResponse(string SharedKey, string AuthenticatorUri, string QrCodeDataUrl);
