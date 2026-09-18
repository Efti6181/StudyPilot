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
})();
