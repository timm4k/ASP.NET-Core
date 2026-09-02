async function request(url, options = {}) {
    const response = await fetch(url, options)
    const payload = response.status === 204 ? null : await response.json().catch(() => null)
    if (!response.ok) {
        throw payload ?? { title: `Request failed with status ${response.status}` }
    }
    return payload
}

function jsonRequest(url, method, payload) {
    return request(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
}

export function getPosts() {
    return request("/posts")
}

export function searchPosts(term) {
    return request(`/posts/q?search=${encodeURIComponent(term)}`)
}

export function getPostsByCategory(slug) {
    return request(`/posts/category/${encodeURIComponent(slug)}`)
}

export function getPost(slugOrId) {
    return request(`/posts/${encodeURIComponent(slugOrId)}`)
}

export function getOwnedPosts(authorKey) {
    return request("/posts/mine", { headers: { "X-Author-Key": authorKey } })
}

export function createPost(payload) {
    return jsonRequest("/posts/add", "POST", payload)
}

export function updatePost(payload) {
    return jsonRequest("/posts/edit", "POST", payload)
}

export function setPostActive(payload) {
    return jsonRequest("/posts/active", "PUT", payload)
}

export function deletePost(id) {
    return request(`/posts/delete/${id}`, { method: "DELETE" })
}

export function getCategories() {
    return request("/categories")
}

export function createCategory(payload) {
    return jsonRequest("/categories/add", "POST", payload)
}

export function updateCategory(payload) {
    return jsonRequest("/categories/edit", "POST", payload)
}

export function deleteCategory(id) {
    return request(`/categories/delete/${id}`, { method: "DELETE" })
}

export async function uploadImage(file) {
    const body = new FormData()
    body.append("image", file)
    const result = await request("/images/upload", { method: "POST", body })
    return result.imageUrl
}
