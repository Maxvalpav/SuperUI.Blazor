/**
 * External Micro Frontend Module: Revenue Chart
 * This module is independent of the main Blazor app.
 */

export async function mount(container, parameters) {
    console.log("Mounting Revenue Chart MFE", parameters);
    
    // 1. Load Chart.js dynamically if not present
    if (!window.Chart) {
        await new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
            script.onload = resolve;
            document.head.appendChild(script);
        });
    }

    const title = parameters.title || "Доход за период";
    const color = parameters.color || "#1890ff";

    container.innerHTML = `
        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid #f0f0f0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
            <h4 style="margin: 0 0 16px 0; font-family: sans-serif; color: #333;">${title}</h4>
            <canvas id="revenueChartCanvas" style="max-height: 250px;"></canvas>
            <div style="margin-top: 12px; font-size: 12px; color: #888; text-align: right;">
                Powered by Chart.js (Dynamic MFE)
            </div>
        </div>
    `;

    const ctx = container.querySelector('#revenueChartCanvas').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн'],
            datasets: [{
                label: 'Выручка (тыс. ₽)',
                data: [12, 19, 3, 5, 2, 3],
                borderColor: color,
                backgroundColor: color + '22',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } }
        }
    });
}

export function unmount(container) {
    container.innerHTML = '';
}
