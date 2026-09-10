import { notesApi } from "./notes-api.js"
import { escapeHtml } from "./ui-utils.js"

const authCard = document.querySelector("#auth-card")
const authForm = document.querySelector("#auth-form")
const authStatus = document.querySelector("#auth-status")
const authSubmit = document.querySelector("#auth-submit")
const notesWorkspace = document.querySelector("#notes-workspace")
const noteForm = document.querySelector("#note-form")
const noteList = document.querySelector("#note-list")
const noteSubmit = document.querySelector("#note-submit")
const noteCancel = document.querySelector("#note-cancel")
const noteStatus = document.querySelector("#note-status")
const sessionUser = document.querySelector("#session-user")

let authMode = "login"
let currentUsername = ""
let editingId = null
let notes = []

function friendlyProblem(problem) {
    const reason = problem.payload?.reason
    const messages = {
        USERNAME_TAKEN: "That username is already in use",
        INVALID_CREDENTIALS: "The username or password is incorrect",
        INVALID_CREDENTIALS_FORMAT: "Use a username without spaces and a password of at least 8 characters",
        INVALID_REFRESH_TOKEN: "Your session ended. Please sign in again",
        INVALID_NOTE: "Add a title and a note before saving",
        NOTE_NOT_FOUND: "That note is no longer available"
    }
    return messages[reason] ?? problem.message ?? "Something went wrong. Please try again"
}

function setMessage(element, message = "", isError = false) {
    element.textContent = message
    element.classList.toggle("error", isError)
}

function setAuthenticated(authenticated) {
    authCard.hidden = authenticated
    notesWorkspace.hidden = !authenticated
    if (authenticated) {
        sessionUser.textContent = currentUsername
    }
}

function handleNoteProblem(problem) {
    const message = friendlyProblem(problem)
    if (["INVALID_REFRESH_TOKEN", "AUTHENTICATION_REQUIRED"].includes(problem.payload?.reason)) {
        notesApi.logout()
        notes = []
        currentUsername = ""
        setAuthenticated(false)
        setAuthMode("login")
        setMessage(authStatus, message, true)
        return
    }

    setMessage(noteStatus, message, true)
}

function renderNotes() {
    noteList.innerHTML = notes.length
        ? notes.map(note => `
            <article class="note-card" data-note-id="${note.id}">
                <div class="note-meta"><span>Private note</span><time>${new Date(note.updatedAt).toLocaleDateString("en", { month: "short", day: "numeric", year: "numeric" })}</time></div>
                <h4>${escapeHtml(note.title)}</h4>
                <p>${escapeHtml(note.content)}</p>
                <div class="note-actions">
                    <button type="button" data-edit>Edit</button>
                    <button type="button" data-delete>Delete</button>
                </div>
            </article>`).join("")
        : '<p class="empty-state">No notes yet. Save your first cup</p>'
}

async function loadNotes() {
    notes = await notesApi.getNotes()
    renderNotes()
}

function resetNoteForm() {
    editingId = null
    noteForm.reset()
    noteSubmit.textContent = "Save note"
    noteCancel.hidden = true
    setMessage(noteStatus)
}

function setAuthMode(mode) {
    authMode = mode
    document.querySelectorAll("[data-auth-mode]").forEach(button => {
        const active = button.dataset.authMode === mode
        button.classList.toggle("active", active)
        button.setAttribute("aria-selected", active)
    })
    authSubmit.textContent = mode === "register" ? "Create account" : "Sign in"
    authForm.elements.password.autocomplete = mode === "register" ? "new-password" : "current-password"
    setMessage(authStatus)
}

document.querySelector(".auth-tabs").addEventListener("click", event => {
    const button = event.target.closest("[data-auth-mode]")
    if (button) {
        setAuthMode(button.dataset.authMode)
    }
})

authForm.addEventListener("submit", async event => {
    event.preventDefault()
    const data = new FormData(authForm)
    const username = data.get("username").trim()
    const password = data.get("password")

    authSubmit.disabled = true
    authSubmit.textContent = authMode === "register" ? "Creating account..." : "Signing in..."
    setMessage(authStatus)

    try {
        await notesApi[authMode](username, password)
        currentUsername = username
        setAuthenticated(true)
        authForm.reset()
        await loadNotes()
    } catch (problem) {
        setMessage(authStatus, friendlyProblem(problem), true)
    } finally {
        authSubmit.disabled = false
        authSubmit.textContent = authMode === "register" ? "Create account" : "Sign in"
    }
})

noteForm.addEventListener("submit", async event => {
    event.preventDefault()
    const data = new FormData(noteForm)
    const title = data.get("title").trim()
    const content = data.get("content").trim()

    noteSubmit.disabled = true
    setMessage(noteStatus, editingId ? "Updating your note..." : "Saving your note...")

    try {
        if (editingId) {
            await notesApi.updateNote(editingId, title, content)
        } else {
            await notesApi.createNote(title, content)
        }
        resetNoteForm()
        await loadNotes()
        setMessage(noteStatus, "Saved")
    } catch (problem) {
        handleNoteProblem(problem)
    } finally {
        noteSubmit.disabled = false
    }
})

noteList.addEventListener("click", async event => {
    const card = event.target.closest("[data-note-id]")
    const note = card && notes.find(candidate => candidate.id === card.dataset.noteId)
    if (!note) {
        return
    }

    if (event.target.matches("[data-edit]")) {
        editingId = note.id
        noteForm.elements.title.value = note.title
        noteForm.elements.content.value = note.content
        noteSubmit.textContent = "Update note"
        noteCancel.hidden = false
        noteForm.scrollIntoView({ behavior: "smooth", block: "center" })
    }

    if (event.target.matches("[data-delete]")) {
        if (!window.confirm("Delete this note?")) {
            return
        }

        event.target.disabled = true
        try {
            await notesApi.deleteNote(note.id)
            await loadNotes()
            setMessage(noteStatus, "Note deleted")
        } catch (problem) {
            handleNoteProblem(problem)
            event.target.disabled = false
        }
    }
})

noteCancel.addEventListener("click", resetNoteForm)

document.querySelector("#logout-button").addEventListener("click", () => {
    notesApi.logout()
    notes = []
    currentUsername = ""
    resetNoteForm()
    renderNotes()
    setAuthenticated(false)
    setAuthMode("login")
})
