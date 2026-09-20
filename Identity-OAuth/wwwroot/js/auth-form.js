"use strict"

import { readJson } from "./api-client.js"
import { select, showMessage } from "./dom.js"

export function bindAuthForm(formId, endpoint) {
    const form = select("#" + formId)
    if (!form) {
        return
    }

    form.addEventListener("submit", async event => {
        event.preventDefault()
        const banner = select("#errorBanner")
        const submit = form.querySelector("[type='submit']")
        const payload = Object.fromEntries(new FormData(form).entries())

        showMessage(banner, "")
        submit.disabled = true

        try {
            const result = await readJson(endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            })
            window.location.assign(result.redirect)
        } catch (error) {
            showMessage(banner, error.message)
            submit.disabled = false
        }
    })
}
