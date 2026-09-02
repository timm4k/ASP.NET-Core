import { getAuthorKey, getAuthorName, rememberAuthorName } from "./author-session.js"
import { createPost, getCategories, getPosts, updatePost, uploadImage } from "./blog-client.js"
import { app, escapeHtml, problemText, setStatus } from "./view-helpers.js"

const maximumImageSizeMegabytes = 8
const maximumImageSize = maximumImageSizeMegabytes * 1024 * 1024
const supportedImageTypes = new Set(["image/jpeg", "image/png", "image/webp"])
const emptyImageName = "No image chosen"
const maximumUrlLength = 2048

function categoryCheckboxes(categories, selectedIds = []) {
    return categories.map(category => `
        <label class="checkbox-item">
            <input type="checkbox" name="categoryIds" value="${category.id}" ${selectedIds.includes(category.id) ? "checked" : ""}>
            <span>${escapeHtml(category.name)}</span>
        </label>`).join("")
}

function postFields(categories, post = {}) {
    const imageUrl = post.imageUrl ?? ""
    const author = post.author ?? getAuthorName()
    return `
        <div class="field">
            <label for="title">Title</label>
            <input id="title" name="title" minlength="3" maxlength="120" value="${escapeHtml(post.title ?? "")}" required>
        </div>
        <div class="field">
            <label for="summary">Summary</label>
            <textarea id="summary" name="summary" minlength="10" maxlength="240" required>${escapeHtml(post.summary ?? "")}</textarea>
        </div>
        <div class="field">
            <label for="content">Content</label>
            <textarea id="content" name="content" minlength="20" maxlength="5000" required>${escapeHtml(post.content ?? "")}</textarea>
        </div>
        <div class="field">
            <label>Image from device</label>
            <div class="file-picker">
                <input id="image-file" name="image-file" type="file" accept="image/jpeg,image/png,image/webp" tabindex="-1">
                <button class="button" id="image-file-button" type="button">Choose image</button>
                <span class="file-picker-name" id="image-file-name">${emptyImageName}</span>
            </div>
            <img id="image-preview" class="image-preview" alt="Image preview" ${imageUrl ? `src="${escapeHtml(imageUrl)}"` : "hidden"}>
            <small class="field-hint">JPG, PNG or WebP · maximum ${maximumImageSizeMegabytes} MB</small>
        </div>
        <div class="field">
            <label for="imageUrl">Or image URL</label>
            <input id="imageUrl" name="imageUrl" maxlength="${maximumUrlLength}" value="${escapeHtml(imageUrl)}" placeholder="https://example.com/image.jpg">
            <small class="field-hint">Use a direct image link, not a webpage link</small>
        </div>
        <div class="field">
            <label for="externalUrl">Listen, watch or source URL</label>
            <input id="externalUrl" name="externalUrl" type="url" maxlength="${maximumUrlLength}" value="${escapeHtml(post.externalUrl ?? "")}" placeholder="https://...">
        </div>
        <div class="field">
            <label for="externalLabel">External link label</label>
            <input id="externalLabel" name="externalLabel" maxlength="40" value="${escapeHtml(post.externalLabel ?? "")}" placeholder="Watch official video">
        </div>
        <div class="field">
            <label for="author">Author</label>
            <input id="author" name="author" minlength="2" maxlength="40" value="${escapeHtml(author)}" placeholder="Your name or band handle" required>
            <small class="field-hint">This browser will privately remember ownership of posts you publish</small>
        </div>
        <fieldset class="field checkbox-grid">
            <legend>Categories</legend>
            ${categoryCheckboxes(categories, post.categories?.map(category => category.id) ?? [])}
        </fieldset>`
}

function postPayload(form, imageUrl, id = null) {
    const data = new FormData(form)
    const payload = {
        title: data.get("title"),
        summary: data.get("summary"),
        content: data.get("content"),
        imageUrl,
        externalUrl: data.get("externalUrl"),
        externalLabel: data.get("externalLabel"),
        author: data.get("author"),
        categoryIds: data.getAll("categoryIds").map(Number)
    }
    return id === null
        ? { ...payload, authorKey: getAuthorKey() }
        : { id, ...payload }
}

function imageLoads(source) {
    return new Promise((resolve, reject) => {
        const image = new Image()
        const timeout = setTimeout(() => reject(new Error("Image loading timed out")), 8000)
        image.onload = () => {
            clearTimeout(timeout)
            resolve()
        }
        image.onerror = () => {
            clearTimeout(timeout)
            reject(new Error("Image could not be loaded"))
        }
        image.src = source
    })
}
export async function renderPostForm(mode) {
    const [categories, posts] = await Promise.all([getCategories(), getPosts()])
    const isEdit = mode === "edit"
    app.innerHTML = `
        <div class="page-heading">
            <div><p class="kicker">Editorial terminal</p><h1>${isEdit ? "Edit file" : "New file"}</h1></div>
            ${isEdit ? "" : `<a class="button" href="/posts/edit">Edit existing</a>`}
        </div>
        <div class="editor-layout">
            <form id="post-form" class="form-card">
                <div id="status" class="status-box"></div>
                ${isEdit ? `
                    <div class="field">
                        <label for="post-select">Post to edit</label>
                        <select id="post-select" required>
                            <option value="">Select a post</option>
                            ${posts.map(post => `<option value="${post.id}">${escapeHtml(post.title)}</option>`).join("")}
                        </select>
                    </div>` : ""}
                <div id="post-fields">${postFields(categories)}</div>
                <div class="form-actions">
                    <button class="button primary" id="post-submit" type="submit">${isEdit ? "Save changes" : "Publish file"}</button>
                    <a class="button" href="/">Cancel</a>
                </div>
            </form>
            <aside class="side-card">
                <p class="kicker">Before broadcast</p>
                <p>Choose a device image or provide a direct image URL. Every post needs a title, summary, content, author and at least one category</p>
                <a class="card-link" href="/my-posts">Manage my posts →</a>
            </aside>
        </div>`

    let selectedPost = null
    let uploadedImageUrl = null
    let previewObjectUrl = null
    const form = document.querySelector("#post-form")
    const status = document.querySelector("#status")
    const submitButton = document.querySelector("#post-submit")

    function showPreview(preview, source) {
        if (!source) {
            preview.removeAttribute("src")
            preview.hidden = true
            return
        }
        preview.src = source
        preview.hidden = false
    }

    function bindPreview() {
        const fileInput = form.querySelector("#image-file")
        const fileButton = form.querySelector("#image-file-button")
        const fileName = form.querySelector("#image-file-name")
        const preview = form.querySelector("#image-preview")
        const urlInput = form.querySelector("#imageUrl")
        if (!fileInput || !fileButton || !fileName || !preview || !urlInput) return

        fileButton.addEventListener("click", () => fileInput.click())
        fileInput.addEventListener("change", () => {
            const [file] = fileInput.files
            if (!file) {
                fileName.textContent = emptyImageName
                return
            }
            if (file.size > maximumImageSize) {
                setStatus(status, `Image must not exceed ${maximumImageSizeMegabytes} MB`)
                fileInput.value = ""
                fileName.textContent = emptyImageName
                return
            }
            if (!supportedImageTypes.has(file.type)) {
                setStatus(status, "Only JPG, PNG and WebP images are allowed")
                fileInput.value = ""
                fileName.textContent = emptyImageName
                return
            }

            if (previewObjectUrl) URL.revokeObjectURL(previewObjectUrl)
            previewObjectUrl = URL.createObjectURL(file)
            uploadedImageUrl = null
            urlInput.value = ""
            fileName.textContent = file.name
            showPreview(preview, previewObjectUrl)
        })

        urlInput.addEventListener("input", () => {
            if (fileInput.files.length) fileInput.value = ""
            if (previewObjectUrl) {
                URL.revokeObjectURL(previewObjectUrl)
                previewObjectUrl = null
            }
            uploadedImageUrl = null
            fileName.textContent = emptyImageName
            showPreview(preview, urlInput.value.trim())
        })
    }

    if (isEdit) {
        document.querySelector("#post-select").addEventListener("change", event => {
            selectedPost = posts.find(post => post.id === Number(event.target.value)) ?? null
            uploadedImageUrl = null
            document.querySelector("#post-fields").innerHTML = postFields(categories, selectedPost ?? {})
            bindPreview()
        })
    }
    bindPreview()

    form.addEventListener("submit", async event => {
        event.preventDefault()
        if (isEdit && !selectedPost) {
            setStatus(status, "Select a post to edit")
            return
        }

        const fileInput = form.querySelector("#image-file")
        const manualUrl = form.elements.imageUrl.value.trim()
        submitButton.disabled = true
        try {
            let imageUrl = uploadedImageUrl
            if (fileInput.files.length) {
                setStatus(status, "Uploading image")
                imageUrl = await uploadImage(fileInput.files[0])
                uploadedImageUrl = imageUrl
                fileInput.value = ""
            } else if (!imageUrl) {
                if (!manualUrl) {
                    throw { errors: { imageUrl: ["Choose an image or enter a direct image URL"] } }
                }
                await imageLoads(manualUrl).catch(() => {
                    throw { errors: { imageUrl: ["Image URL could not be loaded. Use a direct image link"] } }
                })
                imageUrl = manualUrl
            }

            const payload = postPayload(form, imageUrl, isEdit ? selectedPost.id : null)
            const result = await (isEdit ? updatePost(payload) : createPost(payload))
            if (!isEdit) rememberAuthorName(form.elements.author.value)
            setStatus(status, isEdit ? "Post updated" : "Post published", true)
            setTimeout(() => { location.href = `/read/${encodeURIComponent(result.slug)}` }, 450)
        } catch (problem) {
            setStatus(status, problemText(problem))
        } finally {
            submitButton.disabled = false
        }
    })
}
