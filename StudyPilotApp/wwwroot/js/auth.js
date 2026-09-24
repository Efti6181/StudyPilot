(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = button.closest(".sp-password-wrap")?.querySelector("input");
            if (!input) return;
            const showing = input.type === "text";
            input.type = showing ? "password" : "text";
            button.textContent = showing ? "Show" : "Hide";
        });
    });

    const idInput = document.getElementById("UniversityId");
    const idLabel = document.getElementById("universityIdLabel");
    const roleInputs = document.querySelectorAll('input[name="AccountType"]');

    function updateUniversityId() {
        const role = document.querySelector('input[name="AccountType"]:checked')?.value;
        if (!idInput || !idLabel) return;
        if (role === "Student") {
            idLabel.textContent = "Student ID";
            idInput.placeholder = "Enter your Student ID";
        } else if (role === "Faculty") {
            idLabel.textContent = "Faculty ID";
            idInput.placeholder = "Enter your Faculty ID";
        } else {
            idLabel.textContent = "University ID";
            idInput.placeholder = "Select an account type first";
        }
    }

    roleInputs.forEach((input) => input.addEventListener("change", updateUniversityId));
    updateUniversityId();

    document.querySelectorAll("[data-password-strength]").forEach((input) => {
        const panel = input.closest("form")?.querySelector("[data-password-strength-panel]");
        const bar = panel?.querySelector("[data-password-strength-bar]");
        const label = panel?.querySelector("[data-password-strength-label]");
        if (!panel || !bar || !label) return;

        const updateStrength = () => {
            const value = input.value || "";
            const checks = [
                value.length >= 8,
                /[a-z]/.test(value),
                /[A-Z]/.test(value),
                /\d/.test(value),
                /[^A-Za-z0-9]/.test(value)
            ];
            const score = value ? checks.filter(Boolean).length : 0;
            const percent = score === 0 ? 0 : score * 20;
            const strength = score <= 2 ? "Weak" : score <= 3 ? "Fair" : score === 4 ? "Strong" : "Excellent";
            panel.dataset.score = String(score);
            bar.style.width = `${percent}%`;
            label.textContent = value ? `${strength} password` : "Password strength";
        };

        input.addEventListener("input", updateStrength);
        updateStrength();
    });

    const emailInput = document.querySelector("[data-email-availability]");
    const emailMessage = document.querySelector("[data-email-availability-message]");
    let emailTimer;
    let emailRequest;

    emailInput?.addEventListener("input", () => {
        window.clearTimeout(emailTimer);
        emailRequest?.abort();
        const email = emailInput.value.trim();
        if (!emailMessage) return;
        emailMessage.textContent = email ? "Checking email…" : "";
        emailMessage.className = "sp-availability checking";
        if (!email || !emailInput.validity.valid) {
            emailMessage.textContent = email ? "Enter a valid email address." : "";
            emailMessage.className = "sp-availability invalid";
            return;
        }

        emailTimer = window.setTimeout(async () => {
            const endpoint = emailInput.dataset.availabilityUrl;
            if (!endpoint) return;
            emailRequest = new AbortController();
            try {
                const response = await fetch(`${endpoint}?email=${encodeURIComponent(email)}`, {
                    headers: { "Accept": "application/json" },
                    signal: emailRequest.signal
                });
                if (!response.ok) throw new Error("Availability request failed.");
                const result = await response.json();
                if (emailInput.value.trim() !== email) return;
                emailMessage.textContent = result.message;
                emailMessage.className = `sp-availability ${result.isAvailable ? "available" : "unavailable"}`;
            } catch (error) {
                if (error.name === "AbortError") return;
                emailMessage.textContent = "Availability could not be checked right now.";
                emailMessage.className = "sp-availability invalid";
            }
        }, 450);
    });
})();
