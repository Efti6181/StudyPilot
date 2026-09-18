(() => {
    "use strict";

    const charts = [];

    function getThemeColors() {
        const styles = getComputedStyle(document.documentElement);
        return {
            primary: styles.getPropertyValue("--primary").trim() || "#4f46e5",
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
        icon.className = "toast-icon";
        icon.innerHTML = '<i class="bi bi-check2"></i>';

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

    function openModal(title, content) {
        const modalElement = document.getElementById("appModal");
        const titleElement = document.getElementById("appModalTitle");
        const bodyElement = document.getElementById("appModalBody");

        if (!modalElement || !titleElement || !bodyElement || !window.bootstrap) return;

        titleElement.textContent = title;
        bodyElement.replaceChildren(content);
        window.bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    function notificationsContent() {
        const wrapper = document.createElement("div");
        wrapper.className = "d-grid gap-2";

        [
            ["bi-alarm", "Assessment reminder", "Process Scheduling CT is due soon."],
            ["bi-graph-up-arrow", "Progress update", "Your semester GPA trend is improving."],
            ["bi-calendar-event", "Campus event", "The CSE project showcase has been added."]
        ].forEach(([iconName, title, message]) => {
            const row = document.createElement("div");
            row.className = "list-row rounded-3 border";
            row.innerHTML = `<span class="list-icon"><i class="bi ${iconName}"></i></span><div><div class="list-title"></div><div class="list-meta"></div></div>`;
            row.querySelector(".list-title").textContent = title;
            row.querySelector(".list-meta").textContent = message;
            wrapper.appendChild(row);
        });

        return wrapper;
    }

    function messageContent(message) {
        const paragraph = document.createElement("p");
        paragraph.className = "mb-0 text-secondary";
        paragraph.textContent = message;
        return paragraph;
    }

    function initializeCharts() {
        if (!window.Chart) {
            showToast("Dashboard loaded", "Charts require an internet connection the first time.");
            return;
        }

        charts.splice(0).forEach(chart => chart.destroy());
        const colors = getThemeColors();
        const commonScales = {
            x: {
                grid: { display: false },
                border: { display: false },
                ticks: { color: colors.text, font: { family: "Inter", size: 11 } }
            },
            y: {
                grid: { color: colors.grid },
                border: { display: false },
                ticks: { color: colors.text, font: { family: "Inter", size: 11 } }
            }
        };

        const gpaCanvas = document.getElementById("gpaTrendChart");
        if (gpaCanvas) {
            const context = gpaCanvas.getContext("2d");
            const gradient = context.createLinearGradient(0, 0, 0, 260);
            gradient.addColorStop(0, `${colors.primary}45`);
            gradient.addColorStop(1, `${colors.primary}00`);

            charts.push(new Chart(context, {
                type: "line",
                data: {
                    labels: ["Sem 1", "Sem 2", "Sem 3", "Sem 4", "Sem 5", "Sem 6"],
                    datasets: [{
                        label: "GPA",
                        data: [3.08, 3.18, 3.24, 3.31, 3.34, 3.42],
                        borderColor: colors.primary,
                        backgroundColor: gradient,
                        fill: true,
                        borderWidth: 3,
                        tension: .38,
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
                        x: commonScales.x,
                        y: { ...commonScales.y, min: 2.8, max: 4, ticks: { ...commonScales.y.ticks, stepSize: .2 } }
                    }
                }
            }));
        }

        const hoursCanvas = document.getElementById("studyHoursChart");
        if (hoursCanvas) {
            charts.push(new Chart(hoursCanvas, {
                type: "bar",
                data: {
                    labels: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
                    datasets: [{
                        label: "Hours",
                        data: [3.2, 2.1, 2.8, 1.5, 2.4, 1.2, 1.3],
                        backgroundColor: [colors.primary, colors.secondary, colors.primary, colors.secondary, colors.primary, colors.secondary, colors.primary],
                        borderRadius: 7,
                        borderSkipped: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: commonScales.x,
                        y: { ...commonScales.y, beginAtZero: true, suggestedMax: 4 }
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
            sidebar.classList.remove("show");
            backdrop.classList.remove("show");
        };

        menuButton.addEventListener("click", () => {
            sidebar.classList.toggle("show");
            backdrop.classList.toggle("show");
        });
        backdrop.addEventListener("click", closeSidebar);
        window.addEventListener("resize", () => {
            if (window.innerWidth >= 992) closeSidebar();
        });
    }

    function initializeActions() {
        document.querySelectorAll("[data-coming-soon]").forEach(element => {
            element.addEventListener("click", event => {
                event.preventDefault();
                const feature = element.dataset.comingSoon || "This feature";
                showToast(feature, "This module will be connected in the next StudyPilot phase.");
            });
        });

        document.querySelector(".theme-toggle")?.addEventListener("click", () => {
            const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            applyTheme(nextTheme);
            initializeCharts();
        });

        document.getElementById("notificationButton")?.addEventListener("click", () => {
            openModal("Notifications", notificationsContent());
        });

        document.getElementById("syncProgress")?.addEventListener("click", event => {
            const icon = event.currentTarget.querySelector("i");
            icon?.classList.add("spin-once");
            window.setTimeout(() => icon?.classList.remove("spin-once"), 700);
            showToast("Progress synchronized", "Your dashboard is already up to date.");
        });

        document.getElementById("globalSearch")?.addEventListener("keydown", event => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            const query = event.currentTarget.value.trim();
            if (!query) {
                showToast("Search StudyPilot", "Enter a course, resource or event name.");
                return;
            }
            openModal("Search", messageContent(`Search for “${query}” will be connected when the academic modules are implemented.`));
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        const savedTheme = localStorage.getItem("studypilot-theme");
        const preferredTheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
        applyTheme(savedTheme === "dark" || savedTheme === "light" ? savedTheme : preferredTheme);

        initializeSidebar();
        initializeActions();
        initializeCharts();

        window.setTimeout(() => document.getElementById("pageLoader")?.classList.add("is-hidden"), 350);
    });
})();
