using CoffeeOrdering.Models;

namespace CoffeeOrdering.Services;

public static class CoffeeCatalog
{
    public static IReadOnlyList<CoffeeRecipe> Drinks { get; } =
    [
        new("espresso", "Espresso", CoffeeCategories.Black, "Small, intense and crowned with crema", 60,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Espresso_Coffee_01.jpg?width=900"),
        new("doppio", "Doppio", CoffeeCategories.Black, "A double espresso with a deeper finish", 90,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Espresso_Doppio.jpg?width=900"),
        new("ristretto", "Ristretto", CoffeeCategories.Black, "A shorter, sweeter espresso extraction", 40,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Ristretto_-_by_INeedCoffee-CoffeeHero.jpg?width=900"),
        new("lungo", "Lungo", CoffeeCategories.Black, "A longer espresso with a gentle bitterness", 120,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Coffee_Lungo_Extra_Crema_Poznan_(2)_01.jpg?width=900"),
        new("americano", "Americano", CoffeeCategories.Black, "Espresso opened up with hot water", 180,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Espresso_Americano.jpeg?width=900"),
        new("long-black", "Long Black", CoffeeCategories.Black, "Hot water topped with espresso and full crema", 180,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Long_Black_1.jpg?width=900"),
        new("turkish-coffee", "Turkish Coffee", CoffeeCategories.Black, "Slow-brewed, unfiltered and richly aromatic", 120,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/T%C3%BCrk_Kahvesi_-_Bakir_Cezve.jpg?width=900"),

        new("cappuccino", "Cappuccino", CoffeeCategories.Milk, "Espresso, steamed milk and a thick foam cap", 180,
            "https://upload.wikimedia.org/wikipedia/commons/1/16/Classic_Cappuccino.jpg"),
        new("latte", "Caffe Latte", CoffeeCategories.Milk, "Mellow espresso wrapped in silky milk", 240,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Caffe_Latte_at_Pulse_Cafe.jpg?width=900"),
        new("latte-macchiato", "Latte Macchiato", CoffeeCategories.Milk, "Distinct layers of milk, espresso and foam", 240,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Latte_macchiato_2020.jpg?width=900"),
        new("flat-white", "Flat White", CoffeeCategories.Milk, "Double espresso with thin velvety milk", 160,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Flat_white_coffee_with_pretty_feather_pattern.jpg?width=900"),
        new("cortado", "Cortado", CoffeeCategories.Milk, "Equal parts espresso and warm milk", 120,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Cortado_-_LOAM_2023-08-22.jpg?width=900"),
        new("espresso-macchiato", "Espresso Macchiato", CoffeeCategories.Milk, "Espresso marked with a spoon of foam", 80,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Espresso_macchiato_Chiang_Mai.JPG?width=900"),

        new("filter-coffee", "Filter Coffee", CoffeeCategories.SlowBrew, "A clean and comforting everyday brew", 300,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Filter_coffee.jpg?width=900"),
        new("v60", "V60 Pour Over", CoffeeCategories.SlowBrew, "Bright, clear and carefully hand-poured", 280,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Tasty_Coffee_pourover_V60_bloom_2025.jpg?width=900"),
        new("chemex", "Chemex", CoffeeCategories.SlowBrew, "Light-bodied coffee with a delicate aroma", 320,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Die_Chemex_6_Cup.jpg?width=900"),
        new("aeropress", "AeroPress", CoffeeCategories.SlowBrew, "A smooth pressure brew with a clean finish", 250,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Brewing_Coffee_with_AeroPress.jpg?width=900"),
        new("french-press", "French Press", CoffeeCategories.SlowBrew, "Full-bodied and rich from immersion brewing", 350,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/French_press_cafetiere_with_coffee_on_Coffee_Right_in_Brno,_Brno-City_District.jpg?width=900"),

        new("cold-brew", "Cold Brew", CoffeeCategories.Cold, "Slow-steeped, smooth and naturally sweet", 250,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Iced_cold_brew_coffee.jpg?width=900"),
        new("nitro-coffee", "Nitro Cold Brew", CoffeeCategories.Cold, "Cold brew with a soft cascading foam", 250,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Maple_Cream_Cold_Brew_-_Trading_Post_Coffee_Roasters_2026-07-16.jpg?width=900"),
        new("frappe", "Greek Frappe", CoffeeCategories.Cold, "Whipped iced coffee with an airy foam", 260,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Frapp%C3%A9.jpg?width=900"),
        new("iced-latte", "Iced Latte", CoffeeCategories.Cold, "Espresso, cold milk and plenty of ice", 260,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Iced_latte_-_BROR_2025-04-26.jpg?width=900"),

        new("mocha", "Caffe Mocha", CoffeeCategories.Sweet, "Espresso, milk and dark chocolate", 260,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Mocha_coffee.jpg?width=900"),
        new("affogato", "Affogato", CoffeeCategories.Sweet, "Vanilla ice cream drowned in hot espresso", 60,
            "https://commons.wikimedia.org/wiki/Special:Redirect/file/Affogato.JPG?width=900")
    ];
}
