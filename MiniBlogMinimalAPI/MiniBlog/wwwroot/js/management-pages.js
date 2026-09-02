import { getAuthorKey } from "./author-session.js"
import { createCategory, deleteCategory, deletePost, getCategories, getOwnedPosts, getPosts, setPostActive, updateCategory } from "./blog-client.js"
import { app, escapeHtml, problemText, setStatus } from "./view-helpers.js"

export async function renderOwnedPosts() {
    let posts = await getOwnedPosts(getAuthorKey())
    app.innerHTML = `
        <div class="page-heading">
            <div><p class="kicker">Private author controls</p><h1>My posts</h1></div>
            <a class="button button-acid" href="/posts/add">Create post</a>
        </div>
        <div id="status" class="status-box"></div>
        <section id="owned-posts" class="management-list"></section>`

    const list = document.querySelector("#owned-posts")
    const status = document.querySelector("#status")
    const draw = () => {
        list.innerHTML = posts.length ? posts.map(post => `
            <article class="management-row">
                <div>
                    <strong>${post.isActive ? `<a href="/read/${encodeURIComponent(post.slug)}">${escapeHtml(post.title)}</a>` : escapeHtml(post.title)}</strong>
                    <small class="visibility-state ${post.isActive ? "active" : "inactive"}">${post.isActive ? "Visible in the archive" : "Deactivated and hidden"}</small>
                </div>
                <button class="button ${post.isActive ? "danger" : "primary"}" type="button" data-post-id="${post.id}" data-next-active="${!post.isActive}">
                    ${post.isActive ? "Deactivate" : "Reactivate"}
                </button>
            </article>`).join("") : `
            <div class="empty-state">
                <h2>No personal posts yet</h2>
                <p>Posts created in this browser will appear here</p>
            </div>`
    }
    draw()

    list.addEventListener("click", async event => {
        const button = event.target.closest("[data-post-id]")
        if (!button) return

        const id = Number(button.dataset.postId)
        const isActive = button.dataset.nextActive === "true"
        try {
            const updated = await setPostActive({ id, authorKey: getAuthorKey(), isActive })
            posts = posts.map(post => post.id === updated.id ? updated : post)
            draw()
            setStatus(status, isActive ? "Post reactivated" : "Post deactivated", true)
        } catch (problem) {
            setStatus(status, problemText(problem))
        }
    })
}
export async function renderCategoryForm(mode) {
    const categories = await getCategories()
    const isEdit = mode === "edit"
    app.innerHTML = `
        <div class="page-heading">
            <div><p class="kicker">Taxonomy terminal</p><h1>${isEdit ? "Edit channel" : "Channels"}</h1></div>
            <a class="button" href="/categories/delete">Delete categories</a>
        </div>
        <div class="editor-layout">
            <form id="category-form" class="form-card">
                <div id="status" class="status-box"></div>
                ${isEdit ? `
                    <div class="field">
                        <label for="category-select">Category to edit</label>
                        <select id="category-select" required>
                            <option value="">Select a category</option>
                            ${categories.map(category => `<option value="${category.id}">${escapeHtml(category.name)}</option>`).join("")}
                        </select>
                    </div>` : ""}
                <div class="field">
                    <label for="name">New category name</label>
                    <input id="name" name="name" minlength="2" maxlength="40" required>
                </div>
                <div class="form-actions">
                    <button class="button primary" type="submit">${isEdit ? "Save category" : "Add category"}</button>
                    ${isEdit ? `<a class="button" href="/categories/add">Add instead</a>` : `<a class="button" href="/categories/edit">Edit existing</a>`}
                </div>
            </form>
            <aside class="side-card">
                <p class="kicker">Current channels</p>
                <div class="management-list">
                    ${categories.map(category => `
                        <a href="/?category=${encodeURIComponent(category.slug)}">
                            <strong>${escapeHtml(category.name)}</strong><br>
                            <small>${category.postCount} post${category.postCount === 1 ? "" : "s"}</small>
                        </a>`).join("")}
                </div>
            </aside>
        </div>`

    const form = document.querySelector("#category-form")
    const status = document.querySelector("#status")
    const select = document.querySelector("#category-select")
    if (select) {
        select.addEventListener("change", event => {
            const category = categories.find(item => item.id === Number(event.target.value))
            form.elements.name.value = category?.name ?? ""
        })
    }
    form.addEventListener("submit", async event => {
        event.preventDefault()
        const selectedId = select ? Number(select.value) : null
        if (isEdit && !selectedId) {
            setStatus(status, "Select a category to edit")
            return
        }
        try {
            const payload = isEdit
                ? { id: selectedId, name: form.elements.name.value }
                : { name: form.elements.name.value }
            const result = await (isEdit ? updateCategory(payload) : createCategory(payload))
            setStatus(status, `${result.name} ${isEdit ? "updated" : "added"}`, true)
            setTimeout(() => location.reload(), 450)
        } catch (problem) {
            setStatus(status, problemText(problem))
        }
    })
}

export async function renderDeleteList(kind) {
    const isPost = kind === "posts"
    let items = await isPost ? getPosts() : getCategories()
    app.innerHTML = `
        <div class="page-heading">
            <div><p class="kicker">Archive control</p><h1>Delete ${isPost ? "files" : "channels"}</h1></div>
            <a class="button" href="${isPost ? "/posts/add" : "/categories/add"}">Back to editor</a>
        </div>
        <div id="status" class="status-box"></div>
        <section id="management-list" class="management-list"></section>`

    const list = document.querySelector("#management-list")
    const status = document.querySelector("#status")
    const draw = () => {
        list.innerHTML = items.length ? items.map(item => `
            <article class="management-row">
                <div>
                    <strong>${escapeHtml(item.title ?? item.name)}</strong>
                    <small>${isPost ? escapeHtml(item.slug) : `${item.postCount} linked post${item.postCount === 1 ? "" : "s"}`}</small>
                </div>
                <button class="button danger" type="button" data-delete-id="${item.id}">Delete</button>
            </article>`).join("") : `<div class="empty-state"><h2>Nothing left to delete</h2></div>`
    }
    draw()

    list.addEventListener("click", async event => {
        const button = event.target.closest("[data-delete-id]")
        if (!button) return
        const id = Number(button.dataset.deleteId)
        const item = items.find(candidate => candidate.id === id)
        if (!confirm(`Delete ${item.title ?? item.name}?`)) return
        try {
            await (isPost ? deletePost(id) : deleteCategory(id))
            items = items.filter(candidate => candidate.id !== id)
            draw()
            setStatus(status, `${isPost ? "Post" : "Category"} deleted`, true)
        } catch (problem) {
            setStatus(status, problemText(problem))
        }
    })
}
