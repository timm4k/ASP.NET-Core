import { requestJson } from "./http-client.js"

class NotesApi {
    #accessToken = null
    #refreshToken = null
    #refreshPromise = null

    async register(username, password) {
        return this.#authenticate("/register", username, password)
    }

    async login(username, password) {
        return this.#authenticate("/login", username, password)
    }

    logout() {
        this.#accessToken = null
        this.#refreshToken = null
    }

    getNotes() {
        return this.#authorizedRequest("/notes")
    }

    createNote(title, content) {
        return this.#authorizedRequest("/notes", {
            method: "POST",
            body: JSON.stringify({ title, content })
        })
    }

    updateNote(id, title, content) {
        return this.#authorizedRequest(`/notes/${id}`, {
            method: "PUT",
            body: JSON.stringify({ title, content })
        })
    }

    deleteNote(id) {
        return this.#authorizedRequest(`/notes/${id}`, { method: "DELETE" })
    }

    async #authenticate(path, username, password) {
        const tokens = await requestJson(path, {
            method: "POST",
            body: JSON.stringify({ username, password })
        })
        this.#setTokens(tokens)
    }

    async #authorizedRequest(path, options = {}, canRefresh = true) {
        try {
            return await requestJson(path, {
                ...options,
                headers: {
                    ...options.headers,
                    Authorization: `Bearer ${this.#accessToken}`
                }
            })
        } catch (problem) {
            if (problem.status !== 401 || !canRefresh || !this.#refreshToken) {
                throw problem
            }

            await this.#refresh()
            return this.#authorizedRequest(path, options, false)
        }
    }

    async #refresh() {
        if (!this.#refreshPromise) {
            this.#refreshPromise = requestJson("/refresh", {
                method: "POST",
                body: JSON.stringify({ refreshToken: this.#refreshToken })
            }).then(tokens => this.#setTokens(tokens)).finally(() => {
                this.#refreshPromise = null
            })
        }

        return this.#refreshPromise
    }

    #setTokens(tokens) {
        this.#accessToken = tokens.accessToken
        this.#refreshToken = tokens.refreshToken
    }
}

export const notesApi = new NotesApi()
