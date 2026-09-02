export const app = document.querySelector("#app")

export const escapeHtml = value => String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;")

export function problemText(problem) {
    if (problem?.errors) {
        return Object.values(problem.errors).flat().join(" · ")
    }
    return problem?.message ?? problem?.title ?? "The request could not be completed"
}

export function setStatus(element, message, success = false) {
    element.textContent = message
    element.classList.add("visible")
    element.classList.toggle("success", success)
}

export function hasCategory(post, slug) {
    return post.categories.some(category => category.slug === slug)
}

export function tags(categories) {
    return `<div class="tag-row">${categories
        .map(category => `<span class="tag">#${escapeHtml(category.name)}</span>`)
        .join("")}</div>`
}
