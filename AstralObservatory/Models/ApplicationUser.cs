using Microsoft.AspNetCore.Identity;

namespace AstralObservatory.Models;

public sealed class ApplicationUser : IdentityUser
{
    [ProtectedPersonalData]
    public required string Name { get; set; }
    public TwoFactorMethod? PreferredTwoFactorMethod { get; set; }
    public bool EmailAlertsEnabled { get; set; } = true;
}

public enum TwoFactorMethod
{
    Sms,
    Totp
}
