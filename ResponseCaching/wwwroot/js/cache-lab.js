(() => {
    const postId = 1;
    let currentPost = null;
    let validator = null;

    const elements = Object.fromEntries([
        "postTitle",
        "postContent",
        "httpStatus",
        "dataSource",
        "lastModified",
        "updatedAt",
        "resetButton",
        "requestButton",
        "repeatButton",
        "requestMessage",
        "updateForm",
        "titleInput",
        "contentInput",
        "updateButton",
        "updateMessage",
        "traceList",
        "clearTrace"
    ].map(id => [id, document.getElementById(id)]));

    elements.requestButton.addEventListener("click", () => loadPost(false));
    elements.repeatButton.addEventListener("click", () => loadPost(true));
    elements.resetButton.addEventListener("click", resetDemonstration);
    elements.updateForm.addEventListener("submit", updatePost);
    elements.clearTrace.addEventListener("click", () => {
        elements.traceList.innerHTML = '<p class="empty">No requests yet</p>';
    });

    async function loadPost(useValidator) {
        setBusy(true);
        clearMessage(elements.requestMessage);
        try {
            const headers = new Headers();
            if (useValidator && validator) {
                headers.set("If-Modified-Since", validator);
            }

            const response = await fetch(`/api/posts/${postId}`, {
                headers,
                cache: "no-cache"
            });
            const source = response.headers.get("X-Data-Source") || "server";
            const lastModified = response.headers.get("Last-Modified") || validator;

            if (response.status === 304) {
                showStatus("304 Not Modified", "cached");
                elements.dataSource.textContent = source;
                elements.lastModified.textContent = lastModified || "—";
                setMessage(elements.requestMessage, "The browser copy is current and no response body was transferred");
                addTrace(304, source, "Browser reused the existing representation");
                return;
            }

            if (!response.ok) {
                throw await createError(response);
            }

            currentPost = await response.json();
            validator = lastModified;
            renderPost(currentPost, source, lastModified);
            showStatus("200 OK", "success");
            setMessage(elements.requestMessage, source === "database"
                ? "Cache miss SQLite was queried and the result is now cached"
                : "Memory cache answered without a database query");
            addTrace(200, source, source === "database" ? "Post loaded from SQLite" : "Post loaded from memory cache");
        } catch (error) {
            setMessage(elements.requestMessage, error.message, true);
            addTrace("ERR", "client", error.message);
        } finally {
            setBusy(false);
        }
    }

    async function updatePost(event) {
        event.preventDefault();
        elements.updateButton.disabled = true;
        clearMessage(elements.updateMessage);
        try {
            const response = await fetch(`/api/posts/${postId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    title: elements.titleInput.value,
                    content: elements.contentInput.value
                })
            });
            if (!response.ok) {
                throw await createError(response);
            }

            const updated = await response.json();
            currentPost = updated;
            setMessage(elements.updateMessage, "Post updated Request it again to validate the older browser copy");
            addTrace(200, "database", `Post updated at ${formatDate(updated.updatedAt)}`);
        } catch (error) {
            setMessage(elements.updateMessage, error.message, true);
            addTrace("ERR", "client", error.message);
        } finally {
            elements.updateButton.disabled = currentPost === null;
        }
    }

    async function resetDemonstration() {
        setBusy(true);
        try {
            const response = await fetch(`/api/posts/${postId}/reset-cache`, { method: "POST" });
            if (!response.ok) {
                throw await createError(response);
            }

            currentPost = null;
            validator = null;
            elements.postTitle.textContent = "Post is ready to load";
            elements.postContent.textContent = "The next request will miss the cache and query SQLite";
            elements.dataSource.textContent = "—";
            elements.lastModified.textContent = "—";
            elements.updatedAt.textContent = "—";
            elements.titleInput.value = "";
            elements.contentInput.value = "";
            elements.repeatButton.disabled = true;
            elements.updateButton.disabled = true;
            showStatus("Reset", "waiting");
            setMessage(elements.requestMessage, "Server cache cleared The next request will access the database");
            clearMessage(elements.updateMessage);
            addTrace(204, "cache-reset", "Demonstration state cleared");
        } catch (error) {
            setMessage(elements.requestMessage, error.message, true);
        } finally {
            setBusy(false);
        }
    }

    function renderPost(post, source, lastModified) {
        elements.postTitle.textContent = post.title;
        elements.postContent.textContent = post.content;
        elements.dataSource.textContent = source;
        elements.lastModified.textContent = lastModified || "—";
        elements.updatedAt.textContent = formatDate(post.updatedAt);
        elements.titleInput.value = post.title;
        elements.contentInput.value = post.content;
        elements.repeatButton.disabled = false;
        elements.updateButton.disabled = false;
    }

    async function createError(response) {
        const body = await response.json().catch(() => null);
        const validation = body?.errors
            ? Object.entries(body.errors).flatMap(([field, messages]) => messages.map(message => `${field}: ${message}`)).join(" · ")
            : null;
        return new Error(validation || body?.detail || body?.title || "The request could not be completed");
    }

    function addTrace(status, source, detail) {
        elements.traceList.querySelector(".empty")?.remove();
        const entry = document.createElement("div");
        entry.className = "trace-entry";
        entry.innerHTML = `<time>${new Date().toLocaleTimeString("en-GB")}</time><code>HTTP ${escapeHtml(status)}</code><span>${escapeHtml(source)} · ${escapeHtml(detail)}</span>`;
        elements.traceList.prepend(entry);
    }

    function showStatus(text, state) {
        elements.httpStatus.textContent = text;
        elements.httpStatus.className = `status ${state}`;
    }

    function setBusy(busy) {
        elements.resetButton.disabled = busy;
        elements.requestButton.disabled = busy;
        elements.repeatButton.disabled = busy || !validator;
    }

    function setMessage(element, text, error = false) {
        element.textContent = text;
        element.className = `message${error ? " error" : ""}`;
    }

    function clearMessage(element) {
        element.textContent = "";
        element.className = "message";
    }

    function formatDate(value) {
        return new Date(value).toLocaleString("en-GB", {
            dateStyle: "medium",
            timeStyle: "medium"
        });
    }

    function escapeHtml(value) {
        const element = document.createElement("span");
        element.textContent = String(value);
        return element.innerHTML;
    }
})();
