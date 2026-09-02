import { activateReveals } from "./site-shell.js"
import { getCategories, getPost, getPosts, getPostsByCategory, searchPosts } from "./blog-client.js"
import { app, escapeHtml, hasCategory, tags } from "./view-helpers.js"

function featureCard(post) {
    return `
        <article class="feature-card reveal">
            <a class="feature-image-wrap" href="/read/${encodeURIComponent(post.slug)}">
                <img class="feature-image" src="${escapeHtml(post.imageUrl)}" alt="${escapeHtml(post.title)}">
            </a>
            <div class="feature-body">
                ${tags(post.categories)}
                <h3><a href="/read/${encodeURIComponent(post.slug)}">${escapeHtml(post.title)}</a></h3>
                <p>${escapeHtml(post.summary)}</p>
                <a class="card-link" href="/read/${encodeURIComponent(post.slug)}">Open dossier →</a>
            </div>
        </article>`
}

function albumCard(post, index = 0) {
    return `
        <article class="album-card reveal">
            <a class="album-cover-wrap" href="/read/${encodeURIComponent(post.slug)}">
                <img class="album-cover" src="${escapeHtml(post.imageUrl)}" alt="Cover of ${escapeHtml(post.title)}">
                <span class="album-number">REC ${String(index + 1).padStart(2, "0")}</span>
            </a>
            <h3><a href="/read/${encodeURIComponent(post.slug)}">${escapeHtml(post.title)}</a></h3>
            <p>${escapeHtml(post.summary)}</p>
            <a class="card-link" href="/read/${encodeURIComponent(post.slug)}">Read file →</a>
        </article>`
}

function albumYear(post) {
    return Number(post.title.match(/\((\d{4})\)/)?.[1] ?? 9999)
}

export async function renderHome() {
    const parameters = new URLSearchParams(location.search)
    const categorySlug = parameters.get("category")
    const search = parameters.get("search")
    const [categories, allPosts] = await Promise.all([getCategories(), getPosts()])
    let selectedPosts = allPosts
    let resultHeading = "Archive results"

    if (categorySlug) {
        selectedPosts = await getPostsByCategory(categorySlug)
        resultHeading = categories.find(category => category.slug === categorySlug)?.name ?? "Category"
    } else if (search) {
        selectedPosts = await searchPosts(search)
        resultHeading = `Search / ${search}`
    }

    const featurePosts = allPosts.filter(post => !hasCategory(post, "albums"))
    const albumPosts = allPosts
        .filter(post => hasCategory(post, "albums"))
        .sort((left, right) => albumYear(left) - albumYear(right))

    app.innerHTML = `
        <section class="hero reveal">
            <div class="hero-copy">
                <p class="kicker">British virtual band / active since 1998</p>
                <h1 data-text="VIRTUAL BAND. REAL NOISE.">Virtual band.<span>Real noise.</span></h1>
                <p>Gorillaz joins Damon Albarn's music with Jamie Hewlett's animated world. Explore the albums, the four fictional members, the real creators and the collaborators who keep changing the sound</p>
                <div class="hero-actions">
                    <a class="button button-acid" href="#records">Browse records</a>
                    <a class="button button-paper" href="/about">Sources and credits</a>
                </div>
            </div>
            <div class="hero-visual">
                <figure class="hero-photo-frame">
                    <img class="hero-photo" src="/images/gorillaz-characters.jpg" alt="2-D, Noodle, Russel Hobbs and Murdoc Niccals seated together">
                    <figcaption class="hero-caption"><span>Russel / Noodle / 2-D / Murdoc</span><span>Virtual lineup</span></figcaption>
                </figure>
            </div>
        </section>

        <section class="archive-tools reveal">
            <form class="search-form" id="search-form">
                <input name="search" value="${escapeHtml(search ?? "")}" minlength="2" placeholder="Search names, albums, collaborators" aria-label="Search posts" required>
                <button type="submit">Scan archive</button>
            </form>
            <div class="category-strip">
                <a class="category-chip ${!categorySlug ? "active" : ""}" href="/">All files</a>
                ${categories.map(category => `
                    <a class="category-chip ${category.slug === categorySlug ? "active" : ""}" href="/?category=${encodeURIComponent(category.slug)}">
                        ${escapeHtml(category.name)} / ${category.postCount}
                    </a>`).join("")}
            </div>
        </section>

        ${categorySlug || search ? `
            <section class="section-block">
                <div class="section-heading">
                    <div><span class="section-index">Filtered transmission</span><h2>${escapeHtml(resultHeading)}</h2></div>
                    <span class="section-index">${selectedPosts.length} file${selectedPosts.length === 1 ? "" : "s"}</span>
                </div>
                ${selectedPosts.length
                    ? `<div class="result-grid">${selectedPosts.map(albumCard).join("")}</div>`
                    : `<div class="empty-state"><h2>No signal found</h2><p>Try another search or category</p></div>`}
            </section>` : `
            <section class="section-block" id="dossiers">
                <div class="section-heading">
                    <div><span class="section-index">File group 01</span><h2>Band dossier</h2></div>
                    <span class="section-index">Fiction outside / humans inside</span>
                </div>
                <div class="feature-grid">${featurePosts.map(featureCard).join("")}</div>
            </section>

            <section class="section-block" id="records">
                <div class="section-heading">
                    <div><span class="section-index">File group 02</span><h2>Record shelf</h2></div>
                    <span class="section-index">2001 — 2026</span>
                </div>
                <div class="album-grid">${albumPosts.map(albumCard).join("")}</div>
            </section>`}`

    document.querySelector("#search-form").addEventListener("submit", event => {
        event.preventDefault()
        const term = new FormData(event.currentTarget).get("search").trim()
        location.href = `/?search=${encodeURIComponent(term)}`
    })
    activateReveals()
}

export async function renderPost(slug) {
    const post = await getPost(slug)
    const isAlbum = hasCategory(post, "albums")
    document.title = `${post.title} / Kong Signal`
    app.innerHTML = `
        <article class="post-article">
            <a class="back-link" href="/">← Return to archive</a>
            <div class="post-masthead reveal">
                <figure class="post-media ${isAlbum ? "" : "wide"}">
                    <img src="${escapeHtml(post.imageUrl)}" alt="${escapeHtml(post.title)}">
                </figure>
                <header class="post-heading">
                    <p class="kicker">Archive file ${String(post.id).padStart(2, "0")} · by ${escapeHtml(post.author)}</p>
                    <h1 class="post-title">${escapeHtml(post.title)}</h1>
                    <p class="post-summary">${escapeHtml(post.summary)}</p>
                    <div class="post-toolbar">
                        ${tags(post.categories)}
                        ${post.externalUrl ? `<a class="button button-acid" href="${escapeHtml(post.externalUrl)}" target="_blank" rel="noreferrer">${escapeHtml(post.externalLabel || "Open source")} ↗</a>` : ""}
                        <a class="button" href="/my-posts">Manage my posts</a>
                    </div>
                </header>
            </div>
            <div class="post-copy reveal">${escapeHtml(post.content)}</div>
        </article>`
    activateReveals()
}
