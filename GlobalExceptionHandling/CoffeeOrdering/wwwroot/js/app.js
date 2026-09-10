import { orderCoffee } from "./coffee-api.js"
import { isCoffeeQuery, matchesDrinkSearch } from "./menu-search.js"
import { escapeHtml, formatTime } from "./ui-utils.js"

const allCategory = "All"
const messages = {
    INVALID_DRINK_TYPE: "Please choose a coffee from our menu",
    OUT_OF_WATER: "We are refilling the machine. Please try again in a moment",
    BREW_TIMEOUT: "That took longer than expected. Please try once more",
    INTERNAL_ERROR: "We could not make your coffee right now",
    INVALID_RESPONSE: "We could not read the barista's response",
    NETWORK_ERROR: "The coffee bar is not responding. Please try again"
}

const drinkGrid = document.querySelector("#drink-grid")
const categoryFilters = document.querySelector("#category-filters")
const menuSearch = document.querySelector("#menu-search")
const orderDock = document.querySelector("#order-dock")
const orderButton = document.querySelector("#order-button")
const selectedDrink = document.querySelector("#selected-drink")
const selectedImage = document.querySelector("#selected-image")
const recentOrders = document.querySelector("#recent-orders")
const toast = document.querySelector("#toast")
const toastTitle = document.querySelector("#toast-title")
const toastMessage = document.querySelector("#toast-message")

let drinks = []
let selectedName = null
let selectedCategory = allCategory
let orders = []
let toastTimer = null

function visibleDrinks() {
    const query = menuSearch.value
    return drinks.filter(drink => {
        const matchesCategory = selectedCategory === allCategory || drink.category === selectedCategory
        return matchesCategory && matchesDrinkSearch(drink, query)
    })
}

function clearSelection(render = true) {
    selectedName = null
    selectedDrink.textContent = ""
    selectedImage.removeAttribute("src")
    selectedImage.alt = ""
    orderDock.hidden = true

    if (render) {
        renderMenu()
    }
}

function renderFilters() {
    const categories = [allCategory, ...new Set(drinks.map(drink => drink.category))]
    categoryFilters.innerHTML = categories.map(category => `
        <button type="button" data-category="${escapeHtml(category)}"
                class="${category === selectedCategory ? "active" : ""}"
                aria-pressed="${category === selectedCategory}">
            ${escapeHtml(category)}
        </button>`).join("")
}

function emptySearchMessage() {
    const query = menuSearch.value.trim()
    if (!query) {
        return "No drinks are available in this category"
    }

    return isCoffeeQuery(query)
        ? "No such coffee available here yet o(*\uFFE3\u25BD\uFFE3*)\u30D6"
        : "Hmmm.. I don't think that's a coffee (\u273F\u25E1\u203F\u25E1)"
}

function renderMenu() {
    const visible = visibleDrinks()
    if (selectedName && !visible.some(drink => drink.name === selectedName)) {
        clearSelection(false)
    }

    drinkGrid.innerHTML = visible.length
        ? visible.map((drink, index) => `
            <button class="drink-card ${drink.name === selectedName ? "selected" : ""}"
                    type="button" data-drink="${escapeHtml(drink.name)}"
                    role="radio" aria-checked="${drink.name === selectedName}">
                <span class="drink-photo">
                    <img src="${escapeHtml(drink.imageUrl)}" alt="${escapeHtml(drink.displayName)}"
                         loading="${index < 8 ? "eager" : "lazy"}">
                </span>
                <span class="drink-copy">
                    <small>${escapeHtml(drink.category)}</small>
                    <strong>${escapeHtml(drink.displayName)}</strong>
                    <span>${escapeHtml(drink.description)}</span>
                </span>
                <i aria-hidden="true">+</i>
            </button>`).join("")
        : `<p class="menu-message">${escapeHtml(emptySearchMessage())}</p>`
}

function selectDrink(name) {
    const drink = drinks.find(candidate => candidate.name === name)
    if (!drink) {
        return
    }

    selectedName = drink.name
    selectedDrink.textContent = drink.displayName
    selectedImage.src = drink.imageUrl
    selectedImage.alt = drink.displayName
    orderDock.hidden = false
    renderMenu()
}

function showToast(title, message, isError = false) {
    clearTimeout(toastTimer)
    toastTitle.textContent = title
    toastMessage.textContent = message
    toast.classList.toggle("error", isError)
    toast.hidden = false
    requestAnimationFrame(() => toast.classList.add("visible"))
    toastTimer = setTimeout(hideToast, 5000)
}

function hideToast() {
    toast.classList.remove("visible")
    setTimeout(() => {
        if (!toast.classList.contains("visible")) {
            toast.hidden = true
        }
    }, 220)
}

function renderOrders() {
    recentOrders.innerHTML = orders.length
        ? orders.map(order => `
            <article class="recent-order ${order.failed ? "failed" : ""}">
                <img src="${escapeHtml(order.imageUrl)}" alt="">
                <div>
                    <small>${escapeHtml(order.time)}</small>
                    <strong>${escapeHtml(order.name)}</strong>
                    <span>${escapeHtml(order.message)}</span>
                </div>
            </article>`).join("")
        : '<p class="empty-state">Your orders will appear here</p>'
}

function addOrder(drink, message, failed = false) {
    orders.unshift({
        name: drink.displayName,
        imageUrl: drink.imageUrl,
        message,
        failed,
        time: formatTime()
    })
    orders = orders.slice(0, 4)
    renderOrders()
}

async function submitOrder() {
    const drink = drinks.find(candidate => candidate.name === selectedName)
    if (!drink || orderButton.disabled) {
        return
    }

    orderButton.disabled = true
    orderButton.textContent = "Making your coffee..."

    try {
        await orderCoffee(drink.name)
        showToast(`Your ${drink.displayName} is ready`, "Made fresh and ready to enjoy")
        addOrder(drink, "Ready to enjoy")
        clearSelection()
    } catch (problem) {
        const reason = problem.payload?.reason ?? "NETWORK_ERROR"
        const message = messages[reason] ?? problem.payload?.localized_message ?? "Please try again"
        showToast("We could not finish that cup", message, true)
        addOrder(drink, "Please try again", true)
    } finally {
        orderButton.disabled = false
        orderButton.textContent = "Make my coffee"
    }
}

async function loadMenu() {
    try {
        const response = await fetch("/menu", { headers: { Accept: "application/json" } })
        if (!response.ok) {
            throw new Error()
        }

        const catalog = await response.json()
        drinks = catalog.drinks
        renderFilters()
        renderMenu()
    } catch {
        drinkGrid.innerHTML = '<p class="menu-message">The menu is unavailable right now. Please refresh the page</p>'
    }
}

categoryFilters.addEventListener("click", event => {
    const button = event.target.closest("[data-category]")
    if (!button) {
        return
    }

    selectedCategory = button.dataset.category
    renderFilters()
    renderMenu()
})

menuSearch.addEventListener("input", renderMenu)

drinkGrid.addEventListener("click", event => {
    const card = event.target.closest("[data-drink]")
    if (card) {
        selectDrink(card.dataset.drink)
    }
})

drinkGrid.addEventListener("error", event => {
    if (event.target.matches("img")) {
        event.target.closest(".drink-photo").classList.add("image-failed")
        event.target.remove()
    }
}, true)

orderButton.addEventListener("click", submitOrder)
document.querySelector("#clear-selection").addEventListener("click", () => clearSelection())
document.querySelector("#toast-close").addEventListener("click", hideToast)

loadMenu()
