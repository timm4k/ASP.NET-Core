const AstralApi = (() => {
    const accessKey = "astral.accessToken";
    const pendingKey = "astral.pendingToken";

    async function request(path, options = {}, token = sessionStorage.getItem(accessKey)) {
        const headers = new Headers(options.headers || {});
        if (options.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
        if (token) headers.set("Authorization", `Bearer ${token}`);
        const response = await fetch(path, { ...options, headers });
        if (response.status === 204) return null;
        const contentType = response.headers.get("content-type") || "";
        const body = contentType.includes("json") ? await response.json() : await response.text();
        if (!response.ok) {
            const validation = body?.errors
                ? Object.entries(body.errors).map(([field, errors]) => `${readableField(field)}: ${errors.map(cleanText).join(", ")}`).join(" · ")
                : null;
            const message = cleanText(validation || body?.detail || body?.title || "Request could not be completed");
            throw new Error(message);
        }
        return body;
    }

    function requireAccess() {
        if (!sessionStorage.getItem(accessKey)) location.href = "/";
    }

    function logout() {
        sessionStorage.removeItem(accessKey);
        sessionStorage.removeItem(pendingKey);
        location.href = "/";
    }

    function readableField(value) {
        const normalized = value.replace(/^\$\.?/, "").replace(/([a-z])([A-Z])/g, "$1 $2");
        return normalized ? normalized.charAt(0).toUpperCase() + normalized.slice(1) : "Input";
    }

    function cleanText(value) {
        return String(value).trim().replace(/\.$/, "");
    }

    return { request, accessKey, pendingKey, requireAccess, logout };
})();
