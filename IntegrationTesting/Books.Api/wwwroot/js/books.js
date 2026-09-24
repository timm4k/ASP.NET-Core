const apiUrl = "/api/books"
const form = document.querySelector("#book-form")
const bookId = document.querySelector("#book-id")
const title = document.querySelector("#title")
const author = document.querySelector("#author")
const formTitle = document.querySelector("#form-title")
const formMessage = document.querySelector("#form-message")
const cancelEdit = document.querySelector("#cancel-edit")
const bookList = document.querySelector("#book-list")

async function loadBooks() {
    const response = await fetch(apiUrl)
    const books = await response.json()
    bookList.replaceChildren(...books.map(createBookCard))
}

function createBookCard(book) {
    const article = document.createElement("article")
    article.className = "book"
    const details = document.createElement("div")
    const heading = document.createElement("h3")
    const byline = document.createElement("p")
    heading.textContent = book.title
    byline.textContent = book.author
    details.append(heading, byline)

    const actions = document.createElement("div")
    actions.className = "book-actions"
    const edit = document.createElement("button")
    edit.className = "secondary"
    edit.textContent = "Edit"
    edit.addEventListener("click", () => beginEdit(book))
    const remove = document.createElement("button")
    remove.className = "secondary"
    remove.textContent = "Delete"
    remove.addEventListener("click", () => deleteBook(book.id))
    actions.append(edit, remove)
    article.append(details, actions)
    return article
}

function beginEdit(book) {
    bookId.value = book.id
    title.value = book.title
    author.value = book.author
    formTitle.textContent = "Edit book"
    cancelEdit.hidden = false
    title.focus()
}

function resetForm() {
    form.reset()
    bookId.value = ""
    formTitle.textContent = "Add a book"
    formMessage.textContent = ""
    cancelEdit.hidden = true
}

async function deleteBook(id) {
    await fetch(`${apiUrl}/${id}`, { method: "DELETE" })
    if (bookId.value === String(id)) resetForm()
    await loadBooks()
}

form.addEventListener("submit", async event => {
    event.preventDefault()
    formMessage.textContent = ""
    const id = bookId.value
    const response = await fetch(id ? `${apiUrl}/${id}` : apiUrl, {
        method: id ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title: title.value, author: author.value })
    })

    if (!response.ok) {
        const problem = await response.json()
        formMessage.textContent = Object.values(problem.errors ?? {}).flat().join(" ") || "The book could not be saved"
        return
    }

    resetForm()
    await loadBooks()
})

cancelEdit.addEventListener("click", resetForm)
document.querySelector("#refresh").addEventListener("click", loadBooks)
loadBooks().catch(() => { bookList.textContent = "The library is temporarily unavailable" })
