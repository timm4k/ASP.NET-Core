"use strict"

import { readJson } from "./api-client.js"
import { renderBadges, select } from "./dom.js"
import { routes } from "./routes.js"
import { bindLogout } from "./session.js"

function renderDashboard(profile) {
    select("#profileName").textContent = profile.name || "Signed-in user"
    select("#profileEmail").textContent = profile.email || "No email claim"

    const avatar = select("#avatar")
    const placeholder = select("#avatarPlaceholder")

    if (profile.avatar) {
        avatar.src = profile.avatar
        avatar.hidden = false
        placeholder.hidden = true
    } else {
        avatar.hidden = true
        placeholder.hidden = false
        placeholder.textContent = (profile.name || profile.email || "?").charAt(0).toUpperCase()
    }

    const roles = profile.roles ?? []
    renderBadges(select("#roleBadges"), roles)

    const isAdmin = roles.includes("Admin")
    select("#adminLink").hidden = !isAdmin
    select("#accessNote").textContent = isAdmin
        ? "Your company email grants administrator access"
        : "This account has standard access"
}

async function initializeDashboard() {
    try {
        renderDashboard(await readJson(routes.profile))
    } catch {
        window.location.replace(routes.login)
    }
}

bindLogout(select("#logoutButton"))
initializeDashboard()
