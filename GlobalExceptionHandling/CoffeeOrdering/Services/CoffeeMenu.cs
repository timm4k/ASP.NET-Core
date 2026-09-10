using CoffeeOrdering.Contracts;
using CoffeeOrdering.Models;

namespace CoffeeOrdering.Services;

public static class CoffeeMenu
{
    private static readonly IReadOnlyDictionary<string, CoffeeRecipe> Recipes =
        CoffeeCatalog.Drinks.ToDictionary(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> DrinkNames { get; } = Recipes.Keys.Order().ToArray();

    public static bool IsSupported(string drink) => Recipes.ContainsKey(drink);

    public static CoffeeRecipe Get(string drink) => Recipes[drink];

    public static CoffeeMenuResponse GetCatalog() => new(
        CoffeeCatalog.Drinks
            .Select(recipe => new CoffeeMenuItemResponse(
                recipe.Name,
                recipe.DisplayName,
                recipe.Category,
                recipe.Description,
                recipe.ImageUrl))
            .ToArray());
}

public static class CoffeeCategories
{
    public const string Black = "Black Coffee";
    public const string Milk = "Milk Classics";
    public const string SlowBrew = "Slow Brew";
    public const string Cold = "Cold Coffee";
    public const string Sweet = "Something Sweet";
}
