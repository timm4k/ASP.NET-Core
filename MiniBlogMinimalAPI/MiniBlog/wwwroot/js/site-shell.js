document.querySelector(".menu-button")?.addEventListener("click", event => {
    const button = event.currentTarget
    const navigation = document.querySelector("#site-nav")
    const isOpen = navigation.classList.toggle("open")
    button.setAttribute("aria-expanded", String(isOpen))
})

export function activateReveals(root = document) {
    const items = root.querySelectorAll(".reveal")
    if (!("IntersectionObserver" in window)) {
        items.forEach(item => item.classList.add("visible"))
        return
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return
            entry.target.classList.add("visible")
            observer.unobserve(entry.target)
        })
    }, { threshold: 0.08 })
    items.forEach(item => observer.observe(item))
}

function buildTicker(track) {
    const firstGroup = track.querySelector(".ticker-group")
    const source = track.dataset.source ?? firstGroup?.innerHTML
    if (!source) return
    track.dataset.source = source

    const firstHalf = document.createElement("div")
    firstHalf.className = "ticker-group"
    firstHalf.innerHTML = source
    track.replaceChildren(firstHalf)

    const minimumWidth = window.innerWidth + 400
    while (firstHalf.scrollWidth < minimumWidth) {
        firstHalf.insertAdjacentHTML("beforeend", source)
    }

    const secondHalf = firstHalf.cloneNode(true)
    secondHalf.setAttribute("aria-hidden", "true")
    track.append(secondHalf)
    track.style.setProperty("--ticker-duration", `${Math.max(18, firstHalf.scrollWidth / 85)}s`)
}

const tickerTracks = document.querySelectorAll(".ticker-track")
tickerTracks.forEach(buildTicker)

let tickerResizeTimer
window.addEventListener("resize", () => {
    clearTimeout(tickerResizeTimer)
    tickerResizeTimer = setTimeout(() => tickerTracks.forEach(buildTicker), 160)
})

activateReveals()
