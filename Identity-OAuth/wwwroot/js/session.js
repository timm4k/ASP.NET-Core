"use strict"

import { readJson } from "./api-client.js"
import { routes } from "./routes.js"

export function bindLogout(button) {
    button.addEventListener("click", async () => {
        button.disabled = true

        try {
            const result = await readJson(routes.logout, { method: "POST" })
            window.location.assign(result.redirect)
        } catch {
            button.disabled = false
        }
    })
}
