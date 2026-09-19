(() => {
    "use strict";

    const dashboardCharts = [];

    function getThemeColors() {
        const styles = getComputedStyle(document.documentElement);
        return {
            primary: styles.getPropertyValue("--primary").trim() || "#2563eb",
            secondary: styles.getPropertyValue("--secondary").trim() || "#14b8a6",
            text: styles.getPropertyValue("--muted").trim() || "#64748b",
            grid: document.documentElement.dataset.theme === "dark"
                ? "rgba(148, 163, 184, .14)"
                : "rgba(15, 23, 42, .08)"
        };
    }

    function applyTheme(theme) {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem("studypilot-theme", theme);

        const icon = document.querySelector(".theme-toggle i");
        if (icon) {
            icon.className = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";
        }
    }

    function showToast(title, message) {
        const stack = document.getElementById("toastStack");
        if (!stack) return;

        const toast = document.createElement("div");
        toast.className = "app-toast";

        const icon = document.createElement("span");
        icon.className = "toast-symbol";
        icon.innerHTML = '<i class="bi bi-info-lg"></i>';

        const copy = document.createElement("div");
        const heading = document.createElement("strong");
        const detail = document.createElement("small");
        heading.textContent = title;
        detail.textContent = message;
        copy.append(heading, detail);

        toast.append(icon, copy);
        stack.appendChild(toast);
        window.setTimeout(() => {
            toast.classList.add("out");
            window.setTimeout(() => toast.remove(), 250);
        }, 3200);
    }

    function readDashboardData() {
        const element = document.getElementById("dashboardChartData");
        if (!element) return null;

        try {
            return JSON.parse(element.textContent);
        } catch {
            return null;
        }
    }

    function initializeDashboardCharts() {
        const data = readDashboardData();
        if (!data) return;
        if (!window.Chart) {
            showToast("Charts unavailable", "The dashboard data is available, but the chart library could not be loaded.");
            return;
        }

        dashboardCharts.splice(0).forEach(chart => chart.destroy());
        const colors = getThemeColors();
        const commonTicks = { color: colors.text, font: { family: "Inter", size: 11 } };

        const gpaCanvas = document.getElementById("gpaTrendChart");
        if (gpaCanvas && data.gpaLabels?.length) {
            const context = gpaCanvas.getContext("2d");
            const gradient = context.createLinearGradient(0, 0, 0, 260);
            gradient.addColorStop(0, `${colors.primary}45`);
            gradient.addColorStop(1, `${colors.primary}00`);

            dashboardCharts.push(new Chart(context, {
                type: "line",
                data: {
                    labels: data.gpaLabels,
                    datasets: [{
                        label: "GPA",
                        data: data.gpaValues,
                        borderColor: colors.primary,
                        backgroundColor: gradient,
                        fill: true,
                        borderWidth: 3,
                        tension: .35,
                        pointRadius: 4,
                        pointHoverRadius: 6,
                        pointBackgroundColor: colors.primary,
                        pointBorderColor: "#fff",
                        pointBorderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: { intersect: false, mode: "index" },
                    plugins: { legend: { display: false } },
                    scales: {
                        x: { grid: { display: false }, border: { display: false }, ticks: commonTicks },
                        y: {
                            min: 0,
                            max: 4,
                            grid: { color: colors.grid },
                            border: { display: false },
                            ticks: { ...commonTicks, stepSize: .5 }
                        }
                    }
                }
            }));
        }

        const courseCanvas = document.getElementById("courseProgressChart");
        if (courseCanvas && data.courseLabels?.length) {
            dashboardCharts.push(new Chart(courseCanvas, {
                type: "bar",
                data: {
                    labels: data.courseLabels,
                    datasets: [{
                        label: "Progress",
                        data: data.courseValues,
                        backgroundColor: data.courseValues.map((value, index) =>
                            value < 50 ? "#ef4444" : index % 2 === 0 ? colors.primary : colors.secondary),
                        borderRadius: 7,
                        borderSkipped: false
                    }]
                },
                options: {
                    indexAxis: "y",
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: {
                            beginAtZero: true,
                            max: 100,
                            grid: { color: colors.grid },
                            border: { display: false },
                            ticks: { ...commonTicks, callback: value => `${value}%` }
                        },
                        y: { grid: { display: false }, border: { display: false }, ticks: commonTicks }
                    }
                }
            }));
        }
    }

    function initializeSidebar() {
        const sidebar = document.getElementById("appSidebar");
        const menuButton = document.getElementById("mobileMenu");
        const backdrop = document.getElementById("sidebarBackdrop");
        if (!sidebar || !menuButton || !backdrop) return;

        const closeSidebar = () => {
            sidebar.classList.remove("open");
            backdrop.classList.remove("show");
        };

        menuButton.addEventListener("click", () => {
            sidebar.classList.toggle("open");
            backdrop.classList.toggle("show");
        });
        backdrop.addEventListener("click", closeSidebar);
        window.addEventListener("resize", () => {
            if (window.innerWidth >= 992) closeSidebar();
        });
    }

    function initializeActions() {
        document.querySelector(".theme-toggle")?.addEventListener("click", () => {
            const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            applyTheme(nextTheme);
            initializeDashboardCharts();
        });

        document.getElementById("syncProgress")?.closest("form")?.addEventListener("submit", event => {
            const button = event.currentTarget.querySelector("button");
            const icon = button?.querySelector("i");
            icon?.classList.add("spin-once");
            if (button) button.disabled = true;
        });

        document.querySelector(".topbar-search")?.addEventListener("submit", event => {
            const input = event.currentTarget.querySelector('input[name="q"]');
            if (input?.value.trim()) return;
            event.preventDefault();
            input?.focus();
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        const savedTheme = localStorage.getItem("studypilot-theme");
        const preferredTheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
        applyTheme(savedTheme === "dark" || savedTheme === "light" ? savedTheme : preferredTheme);

        initializeSidebar();
        initializeActions();
        initializeDashboardCharts();

        window.setTimeout(() => document.getElementById("pageLoader")?.classList.add("is-hidden"), 350);
    });
})();
