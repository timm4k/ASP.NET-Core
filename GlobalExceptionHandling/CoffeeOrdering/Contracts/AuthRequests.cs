namespace CoffeeOrdering.Contracts;

public sealed record RegisterRequest(string? Username, string? Password);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record RefreshRequest(string? RefreshToken);
