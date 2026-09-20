"use strict"

import { readJson } from "./api-client.js"
import { bindAuthForm } from "./auth-form.js"
import { select, showMessage } from "./dom.js"
import { routes } from "./routes.js"

let googleConfigured = false
const googleUnavailableMessage =
    "Google sign-in is not available yet. Follow the setup steps below and restart the app"

function googleMessageFromUrl() {
    const error = new URLSearchParams(window.location.search).get("error")

    if (error === "google") {
        return googleUnavailableMessage
    }

    if (error === "external") {
        return "Google sign-in could not be completed. Please try again"
    }

    return ""
}

function demoAccountCard(account) {
    const card = document.createElement("article")
    card.className = "demo-account"

    const heading = document.createElement("div")
    heading.className = "demo-account-heading"

    const title = document.createElement("h3")
    title.textContent = account.label

    const email = document.createElement("span")
    email.textContent = account.email

    const description = document.createElement("p")
    description.textContent = account.description

    const button = document.createElement("button")
    button.type = "button"
    button.className = "demo-fill"
    button.textContent = "Sign in as " + account.label
    button.addEventListener("click", async () => {
        button.disabled = true
        showMessage(select("#errorBanner"), "")

        try {
            const result = await readJson(routes.demoLogin, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ accountKey: account.key })
            })
            window.location.assign(result.redirect)
        } catch (error) {
            showMessage(select("#errorBanner"), error.message)
            button.disabled = false
        }
    })

    heading.append(title, email)
    card.append(heading, description, button)
    return card
}

function renderDemoAccounts(accounts) {
    const section = select("#demoAccountsSection")
    const container = select("#demoAccounts")

    if (!accounts?.length) {
        return
    }

    container.replaceChildren(...accounts.map(demoAccountCard))
    section.hidden = false
}

async function initializeLogin() {
    const googleButton = select("#googleLogin")
    const googleNote = select("#googleUnavailable")

    try {
        const info = await readJson(routes.appInfo)
        googleConfigured = info.googleConfigured
        googleButton.disabled = false
        googleNote.hidden = googleConfigured
        select("#googleCallbackUrl").textContent =
            window.location.origin + info.googleCallbackPath
        renderDemoAccounts(info.demoAccounts)
    } catch {
        googleButton.disabled = false
        googleNote.hidden = false
    }
}

function beginGoogleSignIn() {
    if (googleConfigured) {
        window.location.assign(routes.googleSignIn)
        return
    }

    showMessage(
        select("#googleErrorBanner"),
        googleUnavailableMessage
    )
    select("#googleUnavailable").hidden = false
}

showMessage(select("#googleErrorBanner"), googleMessageFromUrl())
bindAuthForm("loginForm", routes.login)
select("#googleLogin").addEventListener("click", beginGoogleSignIn)
initializeLogin()
