import { renderHome, renderPost } from "./archive-pages.js"
import { app, escapeHtml, problemText } from "./view-helpers.js"
import { renderCategoryForm, renderDeleteList, renderOwnedPosts } from "./management-pages.js"
import { renderPostForm } from "./post-editor-page.js"

async function route() {
    const path = location.pathname.replace(/\/$/, "") || "/"
    try {
        if (path === "/") return await renderHome()
        if (path.startsWith("/read/")) return await renderPost(decodeURIComponent(path.slice(6)))
        if (path === "/posts/add") return await renderPostForm("add")
        if (path === "/my-posts") return await renderOwnedPosts()
        if (path === "/posts/edit") return await renderPostForm("edit")
        if (path === "/posts/delete") return await renderDeleteList("posts")
        if (path === "/categories/add") return await renderCategoryForm("add")
        if (path === "/categories/edit") return await renderCategoryForm("edit")
        if (path === "/categories/delete") return await renderDeleteList("categories")
        throw { message: "Page was not found" }
    } catch (problem) {
        app.innerHTML = `<section class="empty-state"><p class="kicker">Signal lost</p><h2>${escapeHtml(problemText(problem))}</h2><a class="button button-acid" href="/">Return home</a></section>`
    }
}

route()
