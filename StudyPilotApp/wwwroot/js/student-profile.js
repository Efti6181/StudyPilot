(() => {
    "use strict";

    const maxImageBytes = 2 * 1024 * 1024;
    const allowedImageTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

    document.addEventListener("DOMContentLoaded", () => {
        const imageInput = document.getElementById("profileImageInput");
        const imagePreview = document.getElementById("profileImagePreview");
        const initials = document.getElementById("profileInitials");
        const selectedName = document.getElementById("selectedImageName");
        const removeImage = document.getElementById("RemoveProfileImage");
        const bio = document.getElementById("Bio");
        const bioCount = document.getElementById("bioCount");
        const department = document.getElementById("DepartmentId");
        const program = document.getElementById("AcademicProgramId");

        const updateBioCount = () => {
            if (bio && bioCount) bioCount.textContent = bio.value.length.toString();
        };

        bio?.addEventListener("input", updateBioCount);
        updateBioCount();

        department?.addEventListener("change", async () => {
            if (!program) return;
            program.innerHTML = '<option value="">Loading programs...</option>';
            program.disabled = true;

            if (!department.value) {
                program.innerHTML = '<option value="">Select program</option>';
                program.disabled = false;
                return;
            }

            try {
                const baseUrl = department.dataset.programsUrl || "/Student/Programs";
                const response = await fetch(`${baseUrl}?departmentId=${encodeURIComponent(department.value)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!response.ok) throw new Error("Unable to load programs.");
                const rows = await response.json();
                program.innerHTML = '<option value="">Select program</option>';
                rows.forEach(row => program.add(new Option(row.label, row.id)));
            } catch {
                program.innerHTML = '<option value="">Programs unavailable</option>';
            } finally {
                program.disabled = false;
            }
        });

        imageInput?.addEventListener("change", () => {
            const file = imageInput.files?.[0];
            if (!file) return;

            if (!allowedImageTypes.has(file.type) || file.size > maxImageBytes) {
                imageInput.value = "";
                if (selectedName) {
                    selectedName.textContent = "Choose a JPG, PNG or WebP image smaller than 2 MB.";
                    selectedName.classList.add("text-danger");
                }
                return;
            }

            if (selectedName) {
                selectedName.textContent = file.name;
                selectedName.classList.remove("text-danger");
            }

            if (removeImage) removeImage.checked = false;

            const reader = new FileReader();
            reader.addEventListener("load", () => {
                if (!imagePreview || typeof reader.result !== "string") return;
                imagePreview.src = reader.result;
                imagePreview.classList.remove("d-none");
                initials?.classList.add("d-none");
            });
            reader.readAsDataURL(file);
        });

        removeImage?.addEventListener("change", () => {
            if (!removeImage.checked || imageInput?.files?.length) return;
            imagePreview?.classList.add("opacity-50");
            if (selectedName) {
                selectedName.textContent = "The current photo will be removed after you save.";
                selectedName.classList.remove("text-danger");
            }
        });
    });
})();
