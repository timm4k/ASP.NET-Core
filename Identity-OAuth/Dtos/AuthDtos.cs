namespace IdentityOAuth.Dtos;

public sealed record RegisterDto(string? Email, string? Password);

public sealed record LoginDto(string? Email, string? Password);

public sealed record DemoLoginDto(string? AccountKey);
