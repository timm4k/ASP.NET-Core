(() => {
    AstralApi.requireAccess();
    const ids = ["profileName", "profileEmail", "profilePhone", "initials", "twoFactorMethod", "smsDeliveryDescription", "emailDeliveryDescription", "logoutButton", "disableTwoFactor", "profileMessage"];
    const elements = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
    const { profileName, profileEmail, profilePhone, initials, twoFactorMethod, smsDeliveryDescription, emailDeliveryDescription, logoutButton, disableTwoFactor, profileMessage } = elements;
    load();

    async function load() {
        try {
            const profile = await AstralApi.request("/profile");
            profileName.textContent = profile.name;
            profileEmail.textContent = profile.email;
            profilePhone.textContent = profile.phoneNumber;
            initials.textContent = profile.name.split(/\s+/).map(part => part[0]).join("").slice(0, 2).toUpperCase();
            twoFactorMethod.textContent = profile.twoFactorMethod ? `Current method: ${profile.twoFactorMethod.toUpperCase()}` : "No method is currently configured";
            smsDeliveryDescription.textContent = profile.smsDeliveryMode === "twilio"
                ? "Real SMS delivery through Twilio is configured"
                : "Development mode shows the generated code in the sign-in form instead of sending a real SMS";
            emailDeliveryDescription.textContent = profile.emailDeliveryMode === "development"
                ? "Development mode writes alert emails to the server log"
                : "Alert emails are delivered to your account address";
        } catch { AstralApi.logout(); }
    }

    logoutButton.addEventListener("click", AstralApi.logout);
    disableTwoFactor.addEventListener("click", async () => {
        try {
            await AstralApi.request("/2fa/disable", { method: "POST" });
            profileMessage.textContent = "Two-factor settings cleared. You can choose a method on the next sign-in";
            profileMessage.className = "message success";
            await load();
        } catch (error) {
            profileMessage.textContent = error.message;
            profileMessage.className = "message error";
        }
    });
})();
(() => {
    AstralApi.requireAccess();
    const ids = ["profileName", "profileEmail", "profilePhone", "initials", "twoFactorMethod", "smsDeliveryDescription", "emailDeliveryDescription", "logoutButton", "disableTwoFactor", "profileMessage"];
    const elements = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
    const { profileName, profileEmail, profilePhone, initials, twoFactorMethod, smsDeliveryDescription, emailDeliveryDescription, logoutButton, disableTwoFactor, profileMessage } = elements;
    load();

    async function load() {
        try {
            const profile = await AstralApi.request("/profile");
            profileName.textContent = profile.name;
            profileEmail.textContent = profile.email;
            profilePhone.textContent = profile.phoneNumber;
            initials.textContent = profile.name.split(/\s+/).map(part => part[0]).join("").slice(0, 2).toUpperCase();
            twoFactorMethod.textContent = profile.twoFactorMethod ? `Current method: ${profile.twoFactorMethod.toUpperCase()}` : "No method is currently configured";
            smsDeliveryDescription.textContent = profile.smsDeliveryMode === "twilio"
                ? "Real SMS delivery through Twilio is configured"
                : "Development mode shows the generated code in the sign-in form instead of sending a real SMS";
            emailDeliveryDescription.textContent = profile.emailDeliveryMode === "development"
                ? "Development mode writes alert emails to the server log"
                : "Alert emails are delivered to your account address";
        } catch { AstralApi.logout(); }
    }

    logoutButton.addEventListener("click", AstralApi.logout);
    disableTwoFactor.addEventListener("click", async () => {
        try {
            await AstralApi.request("/2fa/disable", { method: "POST" });
            profileMessage.textContent = "Two-factor settings cleared. You can choose a method on the next sign-in";
            profileMessage.className = "message success";
            await load();
        } catch (error) {
            profileMessage.textContent = error.message;
            profileMessage.className = "message error";
        }
    });
})();
