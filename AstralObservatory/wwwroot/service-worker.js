self.addEventListener("push", event => {
    const payload = event.data ? event.data.json() : { title: "Astral Observatory", message: "A new observation is available", url: "/observatory.html" };
    event.waitUntil(self.registration.showNotification(payload.title, { body: payload.message, icon: "/icon.svg", badge: "/icon.svg", data: { url: payload.url } }));
});

self.addEventListener("notificationclick", event => {
    event.notification.close();
    event.waitUntil(clients.openWindow(event.notification.data.url || "/observatory.html"));
});
