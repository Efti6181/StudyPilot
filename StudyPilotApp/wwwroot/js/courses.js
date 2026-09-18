(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const description = document.getElementById("Description");
        const descriptionCount = document.getElementById("courseDescriptionCount");
        const courseCode = document.getElementById("CourseCode");

        const updateDescriptionCount = () => {
            if (description && descriptionCount) {
                descriptionCount.textContent = description.value.length.toString();
            }
        };

        description?.addEventListener("input", updateDescriptionCount);
        updateDescriptionCount();

        courseCode?.addEventListener("input", () => {
            const start = courseCode.selectionStart;
            const end = courseCode.selectionEnd;
            courseCode.value = courseCode.value.toUpperCase();
            courseCode.setSelectionRange(start, end);
        });

        const filterButtons = document.querySelectorAll("[data-progress-filter]");
        const courseItems = document.querySelectorAll(".course-item");
        const emptyState = document.getElementById("courseClientEmpty");

        filterButtons.forEach(button => {
            button.addEventListener("click", () => {
                const filter = button.dataset.progressFilter || "all";
                let visibleCount = 0;

                filterButtons.forEach(item => item.classList.remove("active"));
                button.classList.add("active");

                courseItems.forEach(item => {
                    const isVisible = filter === "all" || item.dataset.progressState === filter;
                    item.classList.toggle("d-none", !isVisible);
                    if (isVisible) visibleCount++;
                });

                emptyState?.classList.toggle("show", visibleCount === 0);
            });
        });
    });
})();
