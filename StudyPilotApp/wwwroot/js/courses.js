(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const catalog = document.getElementById("CatalogCourseId");
        const instructor = document.getElementById("FacultyCourseAssignmentId");
        const courseCode = document.getElementById("CourseCode");
        const courseName = document.getElementById("CourseName");
        const credit = document.getElementById("CreditHours");
        const type = document.getElementById("CourseType");
        const typeDisplay = document.getElementById("CourseTypeDisplay");
        const semester = document.getElementById("Semester");
        const term = document.getElementById("AcademicTerm");
        const termDisplay = document.getElementById("AcademicTermDisplay");
        const year = document.getElementById("AcademicYear");
        const description = document.getElementById("Description");

        const setValue = (element, value) => {
            if (element) element.value = value ?? "";
        };

        const showCatalogFacts = () => {
            const option = catalog?.selectedOptions?.[0];
            const hasCourse = option && option.value;
            setValue(courseCode, hasCourse ? option.dataset.code : "");
            setValue(courseName, hasCourse ? option.dataset.name : "");
            setValue(credit, hasCourse ? option.dataset.credit : "");
            setValue(type, hasCourse ? option.dataset.type : "");
            setValue(typeDisplay, hasCourse ? option.dataset.type : "");
            setValue(semester, hasCourse ? option.dataset.semester : "");
            setValue(description, hasCourse ? option.dataset.description : "");
        };

        const showInstructorFacts = () => {
            const option = instructor?.selectedOptions?.[0];
            const hasInstructor = option && option.value;
            setValue(term, hasInstructor ? option.dataset.term : "");
            setValue(termDisplay, hasInstructor ? option.dataset.term : "");
            setValue(year, hasInstructor ? option.dataset.year : "");
        };

        const loadInstructors = async selectedId => {
            if (!catalog || !instructor || !catalog.value) return;
            instructor.disabled = true;
            instructor.innerHTML = '<option value="">Loading instructors...</option>';
            try {
                const baseUrl = catalog.dataset.instructorsUrl || "/Courses/Instructors";
                const response = await fetch(`${baseUrl}?catalogCourseId=${encodeURIComponent(catalog.value)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!response.ok) throw new Error("Unable to load instructors.");
                const rows = await response.json();
                instructor.innerHTML = '<option value="">Select instructor</option>';
                rows.forEach(row => {
                    const option = new Option(row.label, row.id, false, String(row.id) === String(selectedId || ""));
                    option.dataset.term = row.term;
                    option.dataset.year = row.year;
                    option.dataset.section = row.section;
                    instructor.add(option);
                });
                showInstructorFacts();
            } catch {
                instructor.innerHTML = '<option value="">Instructors unavailable</option>';
                showInstructorFacts();
            } finally {
                instructor.disabled = false;
            }
        };

        if (catalog instanceof HTMLSelectElement && instructor instanceof HTMLSelectElement) {
            const initiallySelectedInstructor = instructor.value;
            showCatalogFacts();
            if (catalog.value) loadInstructors(initiallySelectedInstructor);
            catalog.addEventListener("change", () => {
                showCatalogFacts();
                setValue(term, "");
                setValue(termDisplay, "");
                setValue(year, "");
                if (catalog.value) loadInstructors(null);
                else instructor.innerHTML = '<option value="">Select instructor</option>';
            });
            instructor.addEventListener("change", showInstructorFacts);
        }

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
