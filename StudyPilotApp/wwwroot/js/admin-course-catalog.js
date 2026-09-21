(() => {
    const form = document.querySelector('[data-course-catalog-form]');
    if (!form) return;

    const department = form.querySelector('[data-catalog-department]');
    const program = form.querySelector('[data-catalog-program]');
    const baseUrl = form.dataset.programsUrl;
    if (!department || !program || !baseUrl) return;

    department.addEventListener('change', async () => {
        program.innerHTML = '<option value="">Available to all department programs</option>';
        if (!department.value) return;
        program.disabled = true;
        try {
            const response = await fetch(`${baseUrl}?departmentId=${encodeURIComponent(department.value)}`, {
                headers: { Accept: 'application/json' }
            });
            if (!response.ok) return;
            const items = await response.json();
            for (const item of items) {
                const option = document.createElement('option');
                option.value = item.id;
                option.textContent = item.label;
                program.appendChild(option);
            }
        } finally {
            program.disabled = false;
        }
    });
})();
