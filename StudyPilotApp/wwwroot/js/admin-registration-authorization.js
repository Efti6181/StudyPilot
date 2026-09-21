document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("authorizationForm");
    if (!form) return;

    const role = document.getElementById("authorizationRole") || form.querySelector('input[name="Role"]');
    const department = document.getElementById("authorizationDepartment");
    const program = document.getElementById("authorizationProgram");
    const studentFields = [...form.querySelectorAll(".admin-student-field")];

    role?.addEventListener("change", updateRoleFields);
    department?.addEventListener("change", loadPrograms);
    updateRoleFields();

    function currentRole() {
        return role?.value || "Student";
    }

    function updateRoleFields() {
        const isStudent = currentRole() === "Student";
        studentFields.forEach(field => {
            field.hidden = !isStudent;
            field.querySelectorAll("input, select").forEach(input => input.disabled = !isStudent);
        });
    }

    async function loadPrograms() {
        if (!program) return;
        const departmentId = department?.value;
        program.innerHTML = '<option value="">Select a program</option>';
        if (!departmentId || departmentId === "0") return;

        try {
            const url = `${form.dataset.programUrl}?departmentId=${encodeURIComponent(departmentId)}`;
            const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
            if (!response.ok) return;
            const items = await response.json();
            items.forEach(item => {
                const option = document.createElement("option");
                option.value = item.id;
                option.textContent = item.text;
                program.appendChild(option);
            });
        } catch {
            // Server validation remains authoritative if options cannot be refreshed.
        }
    }
});
