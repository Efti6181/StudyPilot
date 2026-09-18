document.addEventListener("DOMContentLoaded", () => {
    const dueDate = document.querySelector('input[name="DueDate"]');
    const assignedDate = document.querySelector('input[name="AssignedDate"]');

    if (!dueDate || !assignedDate) return;

    assignedDate.addEventListener("change", () => {
        if (!assignedDate.value) return;
        dueDate.min = `${assignedDate.value}T00:00`;
    });

    if (assignedDate.value) {
        dueDate.min = `${assignedDate.value}T00:00`;
    }
});
