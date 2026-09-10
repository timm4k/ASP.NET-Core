namespace CoffeeOrdering.Contracts;

public sealed record CoffeeMenuResponse(IReadOnlyCollection<CoffeeMenuItemResponse> Drinks);

public sealed record CoffeeMenuItemResponse(
    string Name,
    string DisplayName,
    string Category,
    string Description,
    string ImageUrl);
