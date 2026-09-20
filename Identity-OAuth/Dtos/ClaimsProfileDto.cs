namespace IdentityOAuth.Dtos;

public sealed record ClaimsProfileDto(
    string? Name,
    string? Email,
    string? Avatar,
    IReadOnlyList<string> Roles);
