namespace CoffeeOrdering.Models;

public sealed record CoffeeRecipe(
    string Name,
    string DisplayName,
    string Category,
    string Description,
    int WaterMilliliters,
    string ImageUrl);
