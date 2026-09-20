"use strict"

import { routes } from "./routes.js"

let antiforgeryTokenPromise = null

function errorMessage(payload, status) {
    if (payload?.errors) {
        return Object.values(payload.errors).flat().join(", ")
    }

    return payload?.error ?? payload?.title ?? "Request failed (" + status + ")"
}

async function antiforgeryToken() {
    if (!antiforgeryTokenPromise) {
        antiforgeryTokenPromise = fetch(routes.antiforgeryToken, {
            headers: { Accept: "application/json" }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error("Security token request failed")
                }

                return response.json()
            })
            .catch(error => {
                antiforgeryTokenPromise = null
                throw error
            })
    }

    return antiforgeryTokenPromise
}

export async function readJson(url, options = {}) {
    const method = (options.method ?? "GET").toUpperCase()
    const headers = new Headers(options.headers)
    headers.set("Accept", "application/json")

    if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
        const securityToken = await antiforgeryToken()
        headers.set(securityToken.headerName, securityToken.token)
    }

    const response = await fetch(url, { ...options, headers })
    const payload = await response.json().catch(() => null)

    if (!response.ok || payload === null) {
        throw new Error(errorMessage(payload, response.status))
    }

    return payload
}
