const counterValue = document.querySelector("#counterValue");
const dateValue = document.querySelector("#dateValue");
const timeValue = document.querySelector("#timeValue");
const objectGallery = document.querySelector("#objectGallery");
const headersResult = document.querySelector("#headersResult");
const profileForm = document.querySelector("#profileForm");
const profileResult = document.querySelector("#profileResult");

async function fetchJson(url, options) {
    const response = await fetch(url, options);
    const data = await response.json();

    if (!response.ok) {
        const message = data.errors?.join(" · ") ?? data.error ?? "Request failed";
        throw new Error(message);
    }

    return data;
}

async function loadCounter() {
    try {
        const data = await fetchJson("/counter");
        counterValue.textContent = data.views.toLocaleString();
    } catch (error) {
        counterValue.textContent = "Error";
    }
}

async function loadClock() {
    dateValue.textContent = "…";
    timeValue.textContent = "…";

    try {
        const [dateData, timeData] = await Promise.all([
            fetchJson("/date"),
            fetchJson("/time")
        ]);
        dateValue.textContent = dateData.date;
        timeValue.textContent = timeData.time;
    } catch (error) {
        dateValue.textContent = "Unavailable";
        timeValue.textContent = "Unavailable";
    }
}

async function loadObjects() {
    try {
        const response = await fetch("/objects");
        if (!response.ok) {
            throw new Error("Object request failed");
        }

        objectGallery.innerHTML = await response.text();
    } catch (error) {
        objectGallery.textContent = "Objects are unavailable";
        objectGallery.classList.add("error-state");
    }
}

function appendHeader(name, value) {
    const row = document.createElement("div");
    const key = document.createElement("strong");
    const content = document.createElement("span");

    row.className = "header-row";
    key.textContent = name;
    content.textContent = value;
    row.append(key, content);
    headersResult.append(row);
}

async function loadHeaders() {
    headersResult.className = "headers-result empty-state";
    headersResult.textContent = "Reading request headers…";

    try {
        const data = await fetchJson("/headers", {
            headers: { "X-Demo-Client": "Purple Middleware UI" }
        });
        headersResult.className = "headers-result";
        headersResult.replaceChildren();

        Object.entries(data.headers).forEach(([name, value]) => appendHeader(name, value));
    } catch (error) {
        headersResult.className = "headers-result empty-state error-state";
        headersResult.textContent = error.message;
    }
}

function appendProfileFact(container, label, value) {
    const fact = document.createElement("div");
    const name = document.createElement("span");
    const content = document.createElement("strong");

    fact.className = "profile-fact";
    name.textContent = label;
    content.textContent = value;
    fact.append(name, content);
    container.append(fact);
}

function renderProfile(profile) {
    const card = document.createElement("div");
    const name = document.createElement("h3");
    const role = document.createElement("p");
    const facts = document.createElement("div");

    card.className = "profile-card";
    card.style.setProperty("--profile-color", profile.favoriteColor);
    name.className = "profile-name";
    name.textContent = `${profile.name} ${profile.surname}`;
    role.className = "profile-role";
    role.textContent = profile.occupation;
    facts.className = "profile-facts";

    appendProfileFact(facts, "Age", profile.age.toString());
    appendProfileFact(facts, "City", profile.city);
    appendProfileFact(facts, "Hobby", profile.hobby);
    appendProfileFact(facts, "Favorite color", profile.favoriteColor);
    card.append(name, role, facts);

    profileResult.className = "profile-result";
    profileResult.replaceChildren(card);
}

async function submitProfile(event) {
    event.preventDefault();
    const query = new URLSearchParams(new FormData(profileForm));

    try {
        const data = await fetchJson(`/profile?${query}`);
        renderProfile(data.profile);
    } catch (error) {
        profileResult.className = "profile-result empty-state error-state";
        profileResult.textContent = error.message;
    }
}

document.querySelector("#counterButton").addEventListener("click", loadCounter);
document.querySelector("#clockButton").addEventListener("click", loadClock);
document.querySelector("#headersButton").addEventListener("click", loadHeaders);
profileForm.addEventListener("submit", submitProfile);

loadClock();
loadObjects();
