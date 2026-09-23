(() => {
    AstralApi.requireAccess();
    const classNames = { Sun: "sun", Moon: "moon", "Halley's Comet": "halley", Mars: "mars", Saturn: "saturn", Andromeda: "andromeda" };
    let profile;
    let objects = [];
    let selected;
    const ids = ["objectList", "spaceStage", "objectType", "objectName", "objectDescription", "metricValue", "metricName", "meterValue", "updatedAt", "sparkline", "alertForm", "targetValue", "detailMessage", "alertList", "syncButton", "notificationList", "notificationBadge", "notificationToggle", "notificationDrawer", "notificationMessage", "drawerClose", "markRead", "testNotification", "pushButton"];
    const elements = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
    const { objectList, spaceStage, objectType, objectName, objectDescription, metricValue, metricName, meterValue, updatedAt, sparkline, alertForm, targetValue, detailMessage, alertList, syncButton, notificationList, notificationBadge, notificationToggle, notificationDrawer, notificationMessage, drawerClose, markRead, testNotification, pushButton } = elements;

    initialize().catch(error => showMessage(error.message, true));

    async function initialize() {
        [profile, objects] = await Promise.all([AstralApi.request("/profile"), AstralApi.request("/api/objects")]);
        renderObjects();
        await selectObject(objects[0]);
        await Promise.all([loadAlerts(), loadNotifications()]);
        connectSse();
    }

    function renderObjects() {
        objectList.innerHTML = "";
        spaceStage.innerHTML = `<div class="orbit-path orbit-inner"></div><div class="orbit-path orbit-middle"></div><div class="orbit-path orbit-outer"></div><div class="comet-path"></div>`;
        objects.forEach(item => {
            const listButton = document.createElement("button");
            listButton.className = "object-button";
            listButton.innerHTML = `<span class="object-dot"></span><span>${escapeHtml(item.name)}</span>`;
            listButton.addEventListener("click", () => selectObject(item));
            listButton.dataset.id = item.id;
            objectList.append(listButton);

            const objectButton = document.createElement("button");
            objectButton.className = `celestial ${classNames[item.name] || ""}`;
            objectButton.textContent = item.name;
            objectButton.dataset.id = item.id;
            objectButton.setAttribute("aria-label", `Inspect ${item.name}`);
            objectButton.addEventListener("click", () => selectObject(item));
            spaceStage.append(objectButton);
        });
    }

    async function selectObject(item) {
        selected = item;
        document.querySelectorAll("[data-id]").forEach(element => element.classList.toggle("active", Number(element.dataset.id) === item.id));
        objectType.textContent = item.type;
        objectName.textContent = item.name;
        objectDescription.textContent = item.description;
        metricValue.textContent = item.currentActivity.toFixed(1);
        metricName.textContent = item.activityMetric;
        meterValue.style.width = `${item.currentActivity}%`;
        updatedAt.textContent = `Updated ${formatTime(item.lastUpdated)}`;
        const history = await AstralApi.request(`/api/objects/${item.id}/observations`);
        sparkline.setAttribute("points", makeSparkline(history.map(point => point.activityLevel)));
    }

    async function refreshObjects() {
        const refreshed = await AstralApi.request("/api/objects");
        const selectedId = selected?.id;
        objects = refreshed;
        document.querySelectorAll(".celestial[data-id]").forEach(element => {
            const item = objects.find(value => value.id === Number(element.dataset.id));
            if (item) element.setAttribute("aria-label", `Inspect ${item.name} at ${item.currentActivity.toFixed(1)}`);
        });
        await selectObject(objects.find(item => item.id === selectedId) || objects[0]);
    }

    async function loadAlerts() {
        const alerts = await AstralApi.request("/api/alerts");
        alertList.innerHTML = alerts.length ? "" : `<p class="muted">No thresholds set yet</p>`;
        alerts.forEach(alert => {
            const row = document.createElement("div");
            row.className = "alert-row";
            row.innerHTML = `<strong>${escapeHtml(alert.objectName)} ≥ ${alert.targetValue}</strong><small>${alert.isActive ? "Monitoring" : `Resting until ${formatTime(alert.reactivateAt)}`}</small><button class="button danger" data-delete-alert="${alert.id}">Remove</button>`;
            alertList.append(row);
        });
        document.querySelectorAll("[data-delete-alert]").forEach(button => button.addEventListener("click", async () => {
            await AstralApi.request(`/api/alerts/${button.dataset.deleteAlert}`, { method: "DELETE" });
            await loadAlerts();
        }));
    }

    alertForm.addEventListener("submit", async event => {
        event.preventDefault();
        if (!selected) return;
        try {
            await AstralApi.request("/api/alerts", { method: "POST", body: JSON.stringify({ celestialObjectId: selected.id, targetValue: Number(targetValue.value) }) });
            targetValue.value = "";
            showMessage("Alert created This object is now monitored");
            await loadAlerts();
        } catch (error) { showMessage(error.message, true); }
    });

    syncButton.addEventListener("click", async () => {
        syncButton.disabled = true;
        syncButton.textContent = "Updating";
        try {
            await AstralApi.request("/api/monitor/run", { method: "POST" });
            await refreshObjects();
            await loadAlerts();
        } catch (error) { showMessage(error.message, true); }
        finally { syncButton.disabled = false; syncButton.textContent = "Update observations"; }
    });

    async function loadNotifications() {
        const items = await AstralApi.request(`/api/notifications/${profile.id}/unread`);
        renderNotifications(items);
    }

    function renderNotifications(items) {
        notificationList.innerHTML = items.length ? "" : `<p class="muted">No unread notifications</p>`;
        items.forEach(item => prependNotification(item));
        notificationBadge.textContent = items.length;
        notificationBadge.classList.toggle("hidden", items.length === 0);
    }

    function prependNotification(item) {
        const empty = notificationList.querySelector(".muted");
        if (empty) empty.remove();
        const card = document.createElement("article");
        card.className = "notification";
        const message = item.message ?? item.Message ?? "Observation alert received";
        const createdAt = item.createdAt ?? item.CreatedAt ?? new Date().toISOString();
        card.innerHTML = `<strong>${escapeHtml(message)}</strong><time>${formatTime(createdAt)}</time>`;
        notificationList.prepend(card);
    }

    async function connectSse() {
        try {
            const response = await fetch("/api/notifications/stream", { headers: { Authorization: `Bearer ${sessionStorage.getItem(AstralApi.accessKey)}` } });
            if (!response.ok || !response.body) return;
            const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
            let buffer = "";
            while (true) {
                const { value, done } = await reader.read();
                if (done) break;
                buffer += value;
                const events = buffer.split("\n\n");
                buffer = events.pop() || "";
                events.forEach(event => {
                    const data = event.split("\n").find(line => line.startsWith("data: "))?.slice(6);
                    if (!data) return;
                    const item = JSON.parse(data);
                    prependNotification(item);
                    const count = Number(notificationBadge.textContent || 0) + 1;
                    notificationBadge.textContent = count;
                    notificationBadge.classList.remove("hidden");
                });
            }
        } catch (error) {
            console.warn("SSE connection ended", error);
        }
    }

    notificationToggle.addEventListener("click", () => notificationDrawer.classList.add("open"));
    drawerClose.addEventListener("click", () => notificationDrawer.classList.remove("open"));
    markRead.addEventListener("click", async () => { await AstralApi.request(`/api/notifications/${profile.id}/mark-read`, { method: "POST" }); await loadNotifications(); });
    testNotification.addEventListener("click", async () => {
        testNotification.disabled = true;
        testNotification.textContent = "Sending";
        try {
            const result = await AstralApi.request("/api/notifications/test", {
                method: "POST",
                body: JSON.stringify({ message: "Test alert: your notification channels are ready" })
            });
            showNotificationMessage(result.push.sent > 0
                ? "Test alert sent to this browser"
                : "Test alert added — enable browser alerts to receive Web Push");
        } catch (error) {
            showNotificationMessage(error.message, true);
        } finally {
            testNotification.disabled = false;
            testNotification.textContent = "Send test alert";
        }
    });
    pushButton.addEventListener("click", enablePush);

    async function enablePush() {
        try {
            if (!("serviceWorker" in navigator) || !("PushManager" in window)) throw new Error("Browser notifications are not supported here");
            const registration = await navigator.serviceWorker.register("/service-worker.js");
            const { publicKey } = await AstralApi.request("/api/push/public-key");
            const subscription = await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: decodeKey(publicKey) });
            const json = subscription.toJSON();
            await AstralApi.request("/api/push/subscribe", { method: "POST", body: JSON.stringify({ endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth }) });
            pushButton.textContent = "Browser alerts enabled";
        } catch (error) { showMessage(error.message, true); }
    }

    function decodeKey(value) {
        const padding = "=".repeat((4 - value.length % 4) % 4);
        const raw = atob((value + padding).replace(/-/g, "+").replace(/_/g, "/"));
        return Uint8Array.from([...raw].map(character => character.charCodeAt(0)));
    }

    function makeSparkline(values) {
        if (!values.length) return "";
        const min = Math.min(...values) - 5;
        const range = Math.max(10, Math.max(...values) - min + 5);
        return values.map((value, index) => `${values.length === 1 ? 150 : index * 300 / (values.length - 1)},${95 - (value - min) * 85 / range}`).join(" ");
    }

    function formatTime(value) {
        if (!value) return "Just now";
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? "Just now" : date.toLocaleString("en-GB", { dateStyle: "medium", timeStyle: "short" });
    }
    function escapeHtml(value) { const element = document.createElement("span"); element.textContent = value; return element.innerHTML; }
    function showMessage(value, error = false) { detailMessage.textContent = value; detailMessage.className = `message ${error ? "error" : "success"}`; }
    function showNotificationMessage(value, error = false) { notificationMessage.textContent = value; notificationMessage.className = `message ${error ? "error" : "success"}`; }
})();
(() => {
    AstralApi.requireAccess();
    const classNames = { Sun: "sun", Moon: "moon", "Halley's Comet": "halley", Mars: "mars", Saturn: "saturn", Andromeda: "andromeda" };
    let profile;
    let objects = [];
    let selected;
    const ids = ["objectList", "spaceStage", "objectType", "objectName", "objectDescription", "metricValue", "metricName", "meterValue", "updatedAt", "sparkline", "alertForm", "targetValue", "detailMessage", "alertList", "syncButton", "notificationList", "notificationBadge", "notificationToggle", "notificationDrawer", "notificationMessage", "drawerClose", "markRead", "testNotification", "pushButton"];
    const elements = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
    const { objectList, spaceStage, objectType, objectName, objectDescription, metricValue, metricName, meterValue, updatedAt, sparkline, alertForm, targetValue, detailMessage, alertList, syncButton, notificationList, notificationBadge, notificationToggle, notificationDrawer, notificationMessage, drawerClose, markRead, testNotification, pushButton } = elements;

    initialize().catch(error => showMessage(error.message, true));

    async function initialize() {
        [profile, objects] = await Promise.all([AstralApi.request("/profile"), AstralApi.request("/api/objects")]);
        renderObjects();
        await selectObject(objects[0]);
        await Promise.all([loadAlerts(), loadNotifications()]);
        connectSse();
    }

    function renderObjects() {
        objectList.innerHTML = "";
        spaceStage.innerHTML = `<div class="orbit-path orbit-inner"></div><div class="orbit-path orbit-middle"></div><div class="orbit-path orbit-outer"></div><div class="comet-path"></div>`;
        objects.forEach(item => {
            const listButton = document.createElement("button");
            listButton.className = "object-button";
            listButton.innerHTML = `<span class="object-dot"></span><span>${escapeHtml(item.name)}</span>`;
            listButton.addEventListener("click", () => selectObject(item));
            listButton.dataset.id = item.id;
            objectList.append(listButton);

            const objectButton = document.createElement("button");
            objectButton.className = `celestial ${classNames[item.name] || ""}`;
            objectButton.textContent = item.name;
            objectButton.dataset.id = item.id;
            objectButton.setAttribute("aria-label", `Inspect ${item.name}`);
            objectButton.addEventListener("click", () => selectObject(item));
            spaceStage.append(objectButton);
        });
    }

    async function selectObject(item) {
        selected = item;
        document.querySelectorAll("[data-id]").forEach(element => element.classList.toggle("active", Number(element.dataset.id) === item.id));
        objectType.textContent = item.type;
        objectName.textContent = item.name;
        objectDescription.textContent = item.description;
        metricValue.textContent = item.currentActivity.toFixed(1);
        metricName.textContent = item.activityMetric;
        meterValue.style.width = `${item.currentActivity}%`;
        updatedAt.textContent = `Updated ${formatTime(item.lastUpdated)}`;
        const history = await AstralApi.request(`/api/objects/${item.id}/observations`);
        sparkline.setAttribute("points", makeSparkline(history.map(point => point.activityLevel)));
    }

    async function refreshObjects() {
        const refreshed = await AstralApi.request("/api/objects");
        const selectedId = selected?.id;
        objects = refreshed;
        document.querySelectorAll(".celestial[data-id]").forEach(element => {
            const item = objects.find(value => value.id === Number(element.dataset.id));
            if (item) element.setAttribute("aria-label", `Inspect ${item.name} at ${item.currentActivity.toFixed(1)}`);
        });
        await selectObject(objects.find(item => item.id === selectedId) || objects[0]);
    }

    async function loadAlerts() {
        const alerts = await AstralApi.request("/api/alerts");
        alertList.innerHTML = alerts.length ? "" : `<p class="muted">No thresholds set yet</p>`;
        alerts.forEach(alert => {
            const row = document.createElement("div");
            row.className = "alert-row";
            row.innerHTML = `<strong>${escapeHtml(alert.objectName)} ≥ ${alert.targetValue}</strong><small>${alert.isActive ? "Monitoring" : `Resting until ${formatTime(alert.reactivateAt)}`}</small><button class="button danger" data-delete-alert="${alert.id}">Remove</button>`;
            alertList.append(row);
        });
        document.querySelectorAll("[data-delete-alert]").forEach(button => button.addEventListener("click", async () => {
            await AstralApi.request(`/api/alerts/${button.dataset.deleteAlert}`, { method: "DELETE" });
            await loadAlerts();
        }));
    }

    alertForm.addEventListener("submit", async event => {
        event.preventDefault();
        if (!selected) return;
        try {
            await AstralApi.request("/api/alerts", { method: "POST", body: JSON.stringify({ celestialObjectId: selected.id, targetValue: Number(targetValue.value) }) });
            targetValue.value = "";
            showMessage("Alert created This object is now monitored");
            await loadAlerts();
        } catch (error) { showMessage(error.message, true); }
    });

    syncButton.addEventListener("click", async () => {
        syncButton.disabled = true;
        syncButton.textContent = "Updating";
        try {
            await AstralApi.request("/api/monitor/run", { method: "POST" });
            await refreshObjects();
            await loadAlerts();
        } catch (error) { showMessage(error.message, true); }
        finally { syncButton.disabled = false; syncButton.textContent = "Update observations"; }
    });

    async function loadNotifications() {
        const items = await AstralApi.request(`/api/notifications/${profile.id}/unread`);
        renderNotifications(items);
    }

    function renderNotifications(items) {
        notificationList.innerHTML = items.length ? "" : `<p class="muted">No unread notifications</p>`;
        items.forEach(item => prependNotification(item));
        notificationBadge.textContent = items.length;
        notificationBadge.classList.toggle("hidden", items.length === 0);
    }

    function prependNotification(item) {
        const empty = notificationList.querySelector(".muted");
        if (empty) empty.remove();
        const card = document.createElement("article");
        card.className = "notification";
        const message = item.message ?? item.Message ?? "Observation alert received";
        const createdAt = item.createdAt ?? item.CreatedAt ?? new Date().toISOString();
        card.innerHTML = `<strong>${escapeHtml(message)}</strong><time>${formatTime(createdAt)}</time>`;
        notificationList.prepend(card);
    }

    async function connectSse() {
        try {
            const response = await fetch("/api/notifications/stream", { headers: { Authorization: `Bearer ${sessionStorage.getItem(AstralApi.accessKey)}` } });
            if (!response.ok || !response.body) return;
            const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
            let buffer = "";
            while (true) {
                const { value, done } = await reader.read();
                if (done) break;
                buffer += value;
                const events = buffer.split("\n\n");
                buffer = events.pop() || "";
                events.forEach(event => {
                    const data = event.split("\n").find(line => line.startsWith("data: "))?.slice(6);
                    if (!data) return;
                    const item = JSON.parse(data);
                    prependNotification(item);
                    const count = Number(notificationBadge.textContent || 0) + 1;
                    notificationBadge.textContent = count;
                    notificationBadge.classList.remove("hidden");
                });
            }
        } catch (error) {
            console.warn("SSE connection ended", error);
        }
    }

    notificationToggle.addEventListener("click", () => notificationDrawer.classList.add("open"));
    drawerClose.addEventListener("click", () => notificationDrawer.classList.remove("open"));
    markRead.addEventListener("click", async () => { await AstralApi.request(`/api/notifications/${profile.id}/mark-read`, { method: "POST" }); await loadNotifications(); });
    testNotification.addEventListener("click", async () => {
        testNotification.disabled = true;
        testNotification.textContent = "Sending";
        try {
            const result = await AstralApi.request("/api/notifications/test", {
                method: "POST",
                body: JSON.stringify({ message: "Test alert: your notification channels are ready" })
            });
            showNotificationMessage(result.push.sent > 0
                ? "Test alert sent to this browser"
                : "Test alert added — enable browser alerts to receive Web Push");
        } catch (error) {
            showNotificationMessage(error.message, true);
        } finally {
            testNotification.disabled = false;
            testNotification.textContent = "Send test alert";
        }
    });
    pushButton.addEventListener("click", enablePush);

    async function enablePush() {
        try {
            if (!("serviceWorker" in navigator) || !("PushManager" in window)) throw new Error("Browser notifications are not supported here");
            const registration = await navigator.serviceWorker.register("/service-worker.js");
            const { publicKey } = await AstralApi.request("/api/push/public-key");
            const subscription = await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: decodeKey(publicKey) });
            const json = subscription.toJSON();
            await AstralApi.request("/api/push/subscribe", { method: "POST", body: JSON.stringify({ endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth }) });
            pushButton.textContent = "Browser alerts enabled";
        } catch (error) { showMessage(error.message, true); }
    }

    function decodeKey(value) {
        const padding = "=".repeat((4 - value.length % 4) % 4);
        const raw = atob((value + padding).replace(/-/g, "+").replace(/_/g, "/"));
        return Uint8Array.from([...raw].map(character => character.charCodeAt(0)));
    }

    function makeSparkline(values) {
        if (!values.length) return "";
        const min = Math.min(...values) - 5;
        const range = Math.max(10, Math.max(...values) - min + 5);
        return values.map((value, index) => `${values.length === 1 ? 150 : index * 300 / (values.length - 1)},${95 - (value - min) * 85 / range}`).join(" ");
    }

    function formatTime(value) {
        if (!value) return "Just now";
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? "Just now" : date.toLocaleString("en-GB", { dateStyle: "medium", timeStyle: "short" });
    }
    function escapeHtml(value) { const element = document.createElement("span"); element.textContent = value; return element.innerHTML; }
    function showMessage(value, error = false) { detailMessage.textContent = value; detailMessage.className = `message ${error ? "error" : "success"}`; }
    function showNotificationMessage(value, error = false) { notificationMessage.textContent = value; notificationMessage.className = `message ${error ? "error" : "success"}`; }
})();
