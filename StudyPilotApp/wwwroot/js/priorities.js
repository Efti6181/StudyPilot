document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".priority-range").forEach(input => {
        const output = input.closest(".range-control")?.querySelector("output");
        if (!output) return;
        const render = () => {
            output.textContent = input.max === "100" ? `${input.value}%` : `${input.value} / 5`;
        };
        input.addEventListener("input", render);
        render();
    });

    const weights = [...document.querySelectorAll(".priority-weight")];
    const total = document.getElementById("weightTotal");
    if (weights.length && total) {
        const renderTotal = () => {
            const value = weights.reduce((sum, input) => sum + (Number.parseFloat(input.value) || 0), 0);
            total.textContent = `${value.toFixed(1).replace(".0", "")}%`;
            total.classList.toggle("valid", Math.abs(value - 100) < 0.001);
            total.classList.toggle("invalid", Math.abs(value - 100) >= 0.001);
        };
        weights.forEach(input => input.addEventListener("input", renderTotal));
        renderTotal();
    }
});
