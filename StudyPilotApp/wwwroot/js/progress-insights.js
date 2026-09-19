document.addEventListener("DOMContentLoaded", () => {
    const dataElement = document.getElementById("progressChartData");
    if (!dataElement || typeof Chart === "undefined") return;

    let data;
    try {
        data = JSON.parse(dataElement.textContent);
    } catch {
        return;
    }

    const styles = getComputedStyle(document.documentElement);
    const textColor = styles.getPropertyValue("--muted").trim() || "#64748b";
    const gridColor = styles.getPropertyValue("--line").trim() || "#e2e8f0";
    const blue = "#2563eb";
    const teal = "#14b8a6";
    const amber = "#f59e0b";
    const red = "#ef4444";

    const commonScales = {
        x: { ticks: { color: textColor }, grid: { display: false } },
        y: { beginAtZero: true, suggestedMax: 100, ticks: { color: textColor }, grid: { color: gridColor } }
    };

    create("snapshotTrendChart", {
        type: "line",
        data: { labels: data.snapshots.labels, datasets: [
            { label: "Course progress", data: data.snapshots.courseProgress, borderColor: blue, backgroundColor: "rgba(37,99,235,.10)", fill: true, tension: .35 },
            { label: "Assessment completion", data: data.snapshots.completionRate, borderColor: teal, backgroundColor: "rgba(20,184,166,.08)", fill: true, tension: .35 }
        ]},
        options: chartOptions(commonScales)
    });

    create("assessmentStatusChart", {
        type: "doughnut",
        data: { labels: ["Completed", "Pending", "Overdue"], datasets: [{ data: [data.assessmentStatus.completed, Math.max(0, data.assessmentStatus.pending - data.assessmentStatus.overdue), data.assessmentStatus.overdue], backgroundColor: [teal, amber, red], borderWidth: 0 }] },
        options: { responsive: true, maintainAspectRatio: false, cutout: "68%", plugins: { legend: { position: "bottom", labels: { color: textColor, boxWidth: 10 } } } }
    });

    create("coursePerformanceChart", {
        type: "bar",
        data: { labels: data.courses.labels, datasets: [
            { label: "Course progress", data: data.courses.progress, backgroundColor: "rgba(37,99,235,.75)", borderRadius: 7 },
            { label: "Assessment average", data: data.courses.scores, backgroundColor: "rgba(20,184,166,.75)", borderRadius: 7 }
        ]},
        options: chartOptions(commonScales)
    });

    create("gpaTrendChart", {
        type: "line",
        data: { labels: data.gpa.labels, datasets: [{ label: "Semester GPA", data: data.gpa.values, borderColor: "#7c3aed", backgroundColor: "rgba(124,58,237,.10)", pointBackgroundColor: "#7c3aed", pointRadius: 4, fill: true, tension: .3 }] },
        options: chartOptions({
            x: commonScales.x,
            y: { beginAtZero: true, suggestedMax: 4, ticks: { color: textColor }, grid: { color: gridColor } }
        })
    });

    create("assessmentTypeChart", {
        type: "bar",
        data: { labels: data.assessmentTypes.labels, datasets: [
            { label: "Average score", data: data.assessmentTypes.scores, backgroundColor: "rgba(124,58,237,.75)", borderRadius: 7 },
            { label: "Completion", data: data.assessmentTypes.completion, backgroundColor: "rgba(245,158,11,.70)", borderRadius: 7 }
        ]},
        options: chartOptions(commonScales)
    });

    function create(id, config) {
        const canvas = document.getElementById(id);
        if (canvas) new Chart(canvas, config);
    }

    function chartOptions(scales) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            scales,
            plugins: { legend: { position: "bottom", labels: { color: textColor, boxWidth: 10 } } }
        };
    }
});
