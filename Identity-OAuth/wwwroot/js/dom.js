"use strict"

export const select = (selector, root = document) => root.querySelector(selector)

export function showMessage(container, message) {
    if (!container) {
        return
    }

    container.textContent = message
    container.hidden = !message
}

export function renderBadges(container, roles) {
    container.replaceChildren()
    const labels = roles.length ? roles : ["Viewer"]

    for (const role of labels) {
        const badge = document.createElement("span")
        badge.className = "badge " + (role === "Admin" ? "badge-admin" : "")
        badge.textContent = role
        container.appendChild(badge)
    }
}
