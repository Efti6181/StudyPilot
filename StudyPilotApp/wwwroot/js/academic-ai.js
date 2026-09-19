(() => {
    "use strict";

    const mode = document.getElementById("aiMode");
    const assessment = document.getElementById("aiAssessment");
    const prompt = document.getElementById("aiPrompt");
    const messages = document.getElementById("aiMessages");

    const syncAssessmentState = () => {
        if (!mode || !assessment) return;
        const required = mode.value === "2";
        assessment.required = required;
        assessment.closest(".form-select")?.classList.toggle("ai-required", required);
    };

    document.querySelectorAll("[data-ai-mode]").forEach(button => {
        button.addEventListener("click", () => {
            if (mode) mode.value = button.dataset.aiMode || "0";
            if (prompt) {
                prompt.value = button.dataset.aiPrompt || "";
                prompt.focus();
                prompt.setSelectionRange(prompt.value.length, prompt.value.length);
            }
            syncAssessmentState();
        });
    });

    mode?.addEventListener("change", syncAssessmentState);
    syncAssessmentState();

    if (messages) messages.scrollTop = messages.scrollHeight;

    document.getElementById("aiComposer")?.addEventListener("submit", event => {
        const button = event.currentTarget.querySelector(".ai-send-button");
        if (!button || !event.currentTarget.checkValidity()) return;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';
    });
})();
