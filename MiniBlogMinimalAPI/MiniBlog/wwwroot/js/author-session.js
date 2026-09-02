const authorKeyStorage = "kongSignalAuthorKey"
const authorNameStorage = "kongSignalAuthorName"

export function getAuthorKey() {
    let key = localStorage.getItem(authorKeyStorage)
    if (key) return key

    const bytes = crypto.getRandomValues(new Uint8Array(32))
    key = Array.from(bytes, value => value.toString(16).padStart(2, "0")).join("")
    localStorage.setItem(authorKeyStorage, key)
    return key
}

export function getAuthorName() {
    return localStorage.getItem(authorNameStorage) ?? ""
}

export function rememberAuthorName(name) {
    localStorage.setItem(authorNameStorage, name.trim())
}
