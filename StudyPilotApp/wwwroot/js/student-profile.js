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

        const updateBioCount = () => {
            if (bio && bioCount) bioCount.textContent = bio.value.length.toString();
        };

        bio?.addEventListener("input", updateBioCount);
        updateBioCount();

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
