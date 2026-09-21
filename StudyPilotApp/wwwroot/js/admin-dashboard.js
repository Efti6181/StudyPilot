document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("adminSidebar");
    const backdrop = document.getElementById("adminBackdrop");
    const menuButton = document.getElementById("adminMenuButton");
    const themeButton = document.getElementById("adminThemeButton");

    const savedTheme = localStorage.getItem("studypilot-admin-theme");
    if (savedTheme === "dark") document.documentElement.dataset.theme = "dark";
    updateThemeIcon();

    menuButton?.addEventListener("click", () => {
        sidebar?.classList.add("open");
        backdrop?.classList.add("show");
    });
    backdrop?.addEventListener("click", closeSidebar);

    themeButton?.addEventListener("click", () => {
        const isDark = document.documentElement.dataset.theme === "dark";
        if (isDark) delete document.documentElement.dataset.theme;
        else document.documentElement.dataset.theme = "dark";
        localStorage.setItem("studypilot-admin-theme", isDark ? "light" : "dark");
        updateThemeIcon();
    });

    renderGrowthChart();

    function closeSidebar() {
        sidebar?.classList.remove("open");
        backdrop?.classList.remove("show");
    }

    function updateThemeIcon() {
        const icon = themeButton?.querySelector("i");
        if (!icon) return;
        icon.className = document.documentElement.dataset.theme === "dark"
            ? "bi bi-sun"
            : "bi bi-moon-stars";
    }

    function renderGrowthChart() {
        const canvas = document.getElementById("adminGrowthChart");
        const dataElement = document.getElementById("adminGrowthData");
        if (!canvas || !dataElement || typeof Chart === "undefined") return;

        let data;
        try { data = JSON.parse(dataElement.textContent || "{}"); }
        catch { return; }

        const textColor = getComputedStyle(document.documentElement).getPropertyValue("--a-muted").trim() || "#6b7890";
        const gridColor = getComputedStyle(document.documentElement).getPropertyValue("--a-line").trim() || "#e5eaf2";
        new Chart(canvas, {
            type: "line",
            data: {
                labels: data.labels || [],
                datasets: [
                    { label: "Students", data: data.students || [], borderColor: "#4f46e5", backgroundColor: "rgba(79,70,229,.12)", fill: true, tension: .35, pointRadius: 3 },
                    { label: "Faculty", data: data.faculty || [], borderColor: "#0f9f91", backgroundColor: "rgba(15,159,145,.08)", fill: true, tension: .35, pointRadius: 3 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: "bottom", labels: { color: textColor, usePointStyle: true, boxWidth: 7 } } },
                scales: {
                    x: { grid: { display: false }, ticks: { color: textColor } },
                    y: { beginAtZero: true, ticks: { color: textColor, precision: 0 }, grid: { color: gridColor } }
                }
            }
        });
    }
});
