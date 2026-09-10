const coffeeTerms = [
    "coffee",
    "espresso",
    "doppio",
    "ristretto",
    "lungo",
    "americano",
    "long black",
    "turkish coffee",
    "cappuccino",
    "latte",
    "macchiato",
    "flat white",
    "cortado",
    "breve",
    "galao",
    "filter coffee",
    "batch brew",
    "pour over",
    "v60",
    "chemex",
    "kalita",
    "aeropress",
    "french press",
    "siphon",
    "cold brew",
    "nitro",
    "frappe",
    "freddo",
    "mocha",
    "raf",
    "affogato",
    "glace",
    "con panna",
    "viennese",
    "vietnamese coffee",
    "kopi luwak",
    "dalgona",
    "irish coffee",
    "espresso martini",
    "corretto",
    "bumble",
    "espresso tonic",
    "mazagran"
]

function words(value) {
    return value
        .trim()
        .toLowerCase()
        .replaceAll("-", " ")
        .match(/[a-z0-9]+/g) ?? []
}

export function matchesDrinkSearch(drink, query) {
    const queryWords = words(query)
    if (queryWords.length === 0) {
        return true
    }

    const drinkWords = words(`${drink.displayName} ${drink.description} ${drink.name}`)
    return queryWords.every(queryWord =>
        drinkWords.some(drinkWord => drinkWord.startsWith(queryWord)))
}

export function isCoffeeQuery(value) {
    const queryWords = words(value)
    if (queryWords.length === 0) {
        return false
    }

    return coffeeTerms.some(term => {
        const termWords = words(term)
        return queryWords.every(queryWord =>
            termWords.some(termWord => termWord.startsWith(queryWord)))
    })
}
