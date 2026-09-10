export function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;")
}

export function formatTime(value = new Date()) {
    return new Intl.DateTimeFormat("en", {
        hour: "numeric",
        minute: "2-digit"
    }).format(value)
}
