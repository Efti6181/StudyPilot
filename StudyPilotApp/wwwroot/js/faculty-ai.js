(() => {
    "use strict";

    const mode = document.getElementById("facultyAiMode");
    const course = document.getElementById("facultyAiCourse");
    const prompt = document.getElementById("facultyAiPrompt");
    const messages = document.getElementById("facultyAiMessages");

    const syncCourseRequirement = () => {
        if (!mode || !course) return;
        const required = mode.value === "1" || mode.value === "2";
        course.required = required;
        course.classList.toggle("required", required);
    };

    document.querySelectorAll("[data-faculty-ai-mode]").forEach(button => {
        button.addEventListener("click", () => {
            if (mode) mode.value = button.dataset.facultyAiMode || "0";
            if (prompt) {
                prompt.value = button.dataset.facultyAiPrompt || "";
                prompt.focus();
                prompt.setSelectionRange(prompt.value.length, prompt.value.length);
            }
            syncCourseRequirement();
        });
    });

    mode?.addEventListener("change", syncCourseRequirement);
    syncCourseRequirement();
    if (messages) messages.scrollTop = messages.scrollHeight;

    document.getElementById("facultyAiComposer")?.addEventListener("submit", event => {
        const button = event.currentTarget.querySelector(".faculty-ai-send");
        if (!button || !event.currentTarget.checkValidity()) return;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';
    });
})();
