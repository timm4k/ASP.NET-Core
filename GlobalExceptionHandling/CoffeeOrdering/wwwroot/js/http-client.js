export class ApiProblem extends Error {
    constructor(status, payload) {
        super(payload.localized_message ?? payload.detail ?? "The request failed")
        this.status = status
        this.payload = payload
    }
}

export async function requestJson(path, options = {}) {
    const response = await fetch(path, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            Accept: "application/json",
            ...options.headers
        }
    })
    const payload = response.status === 204 ? null : await response.json().catch(() => ({}))

    if (!response.ok) {
        throw new ApiProblem(response.status, payload)
    }

    return payload
}
