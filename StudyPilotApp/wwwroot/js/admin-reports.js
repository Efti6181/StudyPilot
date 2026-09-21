(() => {
    const source = document.getElementById("adminReportsData");
    if (!source || typeof Chart === "undefined") return;

    let data;
    try { data = JSON.parse(source.textContent); } catch { return; }

    const styles = getComputedStyle(document.documentElement);
    const ink = styles.getPropertyValue("--a-ink").trim() || "#172033";
    const muted = styles.getPropertyValue("--a-muted").trim() || "#6b7890";
    const line = styles.getPropertyValue("--a-line").trim() || "#e5eaf2";
    Chart.defaults.color = muted;
    Chart.defaults.font.family = "Inter, ui-sans-serif, system-ui, sans-serif";

    const commonScales = {
        x: { grid: { display: false }, ticks: { color: muted } },
        y: { beginAtZero: true, grid: { color: line }, ticks: { color: muted, precision: 0 } }
    };
    const growth = document.getElementById("adminGrowthChart");
    if (growth) new Chart(growth, {
        type: "line",
        data: { labels: data.growthLabels, datasets: [
            { label: "Students", data: data.students, borderColor: "#4f46e5", backgroundColor: "rgba(79,70,229,.12)", fill: true, tension: .35 },
            { label: "Faculty", data: data.faculty, borderColor: "#0f9f91", backgroundColor: "rgba(15,159,145,.08)", fill: true, tension: .35 }
        ]},
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { labels: { color: ink, usePointStyle: true } } }, scales: commonScales }
    });

    const role = document.getElementById("adminRoleChart");
    if (role) new Chart(role, {
        type: "doughnut",
        data: { labels: data.roleLabels, datasets: [{ data: data.roleCounts, backgroundColor: ["#4f46e5", "#0f9f91", "#f59e0b"], borderWidth: 0 }] },
        options: { responsive: true, maintainAspectRatio: false, cutout: "68%", plugins: { legend: { position: "bottom", labels: { color: ink, usePointStyle: true } } } }
    });

    const content = document.getElementById("adminContentChart");
    if (content) new Chart(content, {
        type: "bar",
        data: { labels: data.contentLabels, datasets: [{ label: "Records", data: data.contentCounts, backgroundColor: ["#6366f1", "#38bdf8", "#14b8a6", "#f59e0b"], borderRadius: 8 }] },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: commonScales }
    });
})();
