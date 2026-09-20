"use strict"

import { readJson } from "./api-client.js"
import { select } from "./dom.js"
import { routes } from "./routes.js"
import { bindLogout } from "./session.js"

async function initializeAdminPanel() {
    try {
        const profile = await readJson(routes.adminProfile)
        select("#adminEmail").textContent = profile.email
        select("#adminRoles").textContent = profile.roles.join(", ")
        select("#cookieLifetime").textContent = profile.cookieLifetimeMinutes + " minutes"
        select("#policyExplanation").textContent =
            "Authenticated emails ending in " + profile.adminDomain
            + " receive the Admin role through IClaimsTransformation"
    } catch {
        window.location.replace(routes.accessDenied)
    }
}

bindLogout(select("#logoutButton"))
initializeAdminPanel()
