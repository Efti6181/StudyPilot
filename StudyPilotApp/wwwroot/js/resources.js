(() => {
    "use strict";

    const sourceInputs = [...document.querySelectorAll('input[name="Kind"]')];
    const fileFields = document.getElementById("fileSourceFields");
    const linkFields = document.getElementById("linkSourceFields");
    const upload = document.getElementById("Upload");
    const uploadName = document.getElementById("uploadFileName");

    function refreshSource() {
        const selected = sourceInputs.find(input => input.checked)?.value;
        const isFile = selected === "File" || selected === "1";
        if (fileFields) fileFields.hidden = !isFile;
        if (linkFields) linkFields.hidden = isFile;
    }

    sourceInputs.forEach(input => input.addEventListener("change", refreshSource));
    upload?.addEventListener("change", () => {
        if (uploadName) uploadName.textContent = upload.files?.[0]?.name || "Choose a file";
    });

    refreshSource();
})();
