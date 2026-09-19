document.addEventListener("DOMContentLoaded", () => {
    const container = document.getElementById("calculatorRows");
    const addButton = document.getElementById("addCalculatorRow");
    const options = Array.isArray(window.studyPilotGradeOptions)
        ? window.studyPilotGradeOptions
        : [];

    if (!container || !addButton) return;

    addButton.addEventListener("click", () => {
        const index = container.querySelectorAll(".calculator-row").length;
        if (index >= 12) return;

        const row = document.createElement("div");
        row.className = "calculator-row";

        const optionMarkup = options
            .map(option => `<option value="${escapeHtml(option.value)}">${escapeHtml(option.text)}</option>`)
            .join("");

        row.innerHTML = `
            <div><input name="Rows[${index}].CourseName" class="form-control" maxlength="100" placeholder="Course name (optional)" /></div>
            <div><input name="Rows[${index}].Credits" class="form-control" type="number" min="0.5" max="20" step="0.5" placeholder="3" /></div>
            <div><select name="Rows[${index}].LetterGrade" class="form-select"><option value="">Grade</option>${optionMarkup}</select></div>`;
        container.appendChild(row);
    });

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }
});
