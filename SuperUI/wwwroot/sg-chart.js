// SgChart - Chart.js Integration Module
// Provides JavaScript interop for Blazor SgChart component

const chartInstances = new Map();

async function waitForChart() {
    let Chart = window.Chart;
    let attempts = 0;
    while (!Chart && attempts < 50) {
        await new Promise(resolve => setTimeout(resolve, 100));
        Chart = window.Chart;
        attempts++;
    }
    if (!Chart) throw new Error('Chart.js library not loaded');
    return Chart;
}

function applyOptionalPlugins(Chart, config) {
    if (window.ChartZoom) {
        try { Chart.register(window.ChartZoom); } catch { }
        config.options ??= {};
        config.options.plugins ??= {};
        config.options.plugins.zoom = {
            zoom: {
                wheel: { enabled: true, speed: 0.1 },
                pinch: { enabled: true },
                mode: 'y'
            },
            pan: { enabled: true, mode: 'y' }
        };
    }
    if (window.MatrixController) {
        try { Chart.register(window.MatrixController); } catch { }
    }
}

export async function initChart(dotnetRef, canvasRef, config) {
    const Chart = await waitForChart();
    applyOptionalPlugins(Chart, config);

    const ctx = canvasRef.getContext('2d');
    if (!ctx) throw new Error('Failed to get canvas context');

    const chart = new Chart(ctx, config);
    const chartId = canvasRef.id;

    const onClick = (e) => {
        const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
        if (points.length === 0) return;
        const point = points[0];
        const datasetIndex = point.datasetIndex;
        const index = point.index;
        const ds = chart.data.datasets[datasetIndex];
        const raw = ds?.data?.[index];
        const value = typeof raw === 'object' && raw !== null ? (raw.y ?? raw.v ?? 0) : (raw ?? 0);
        const label = chart.data.labels?.[index] ?? '';
        try {
            dotnetRef.invokeMethodAsync('OnDataPointClickedAsync', {
                datasetIndex,
                dataPointIndex: index,
                value,
                label
            });
        } catch { }
    };
    canvasRef.addEventListener('click', onClick);

    chartInstances.set(chartId, {
        instance: chart,
        dotnetRef,
        canvasRef,
        onClick
    });
}

export function updateChart(chartId, config) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;
    chart.data = config.data;
    chart.options = config.options;
    chart.update();
}

export function zoomY(chartId, min, max) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;
    if (chart.options?.scales?.y) {
        chart.options.scales.y.min = min;
        chart.options.scales.y.max = max;
        chart.update();
    }
}

export function resetZoom(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;
    if (chart.options?.scales?.y) {
        delete chart.options.scales.y.min;
        delete chart.options.scales.y.max;
        chart.update();
    }
    if (typeof chart.resetZoom === 'function') {
        try { chart.resetZoom(); } catch { }
    }
}

export function getImageDataUrl(chartId, format = 'png') {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return null;
    const canvas = chartData.canvasRef;
    const mimeType = format === 'jpg' || format === 'jpeg' ? 'image/jpeg' : 'image/png';
    return canvas.toDataURL(mimeType);
}

export function exportImage(chartId, format = 'png') {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const canvas = chartData.canvasRef;
    const link = document.createElement('a');
    const ext = (format === 'jpg' || format === 'jpeg') ? 'jpg' : 'png';
    const mimeType = ext === 'jpg' ? 'image/jpeg' : 'image/png';
    link.href = canvas.toDataURL(mimeType);
    link.download = `chart-${Date.now()}.${ext}`;
    link.click();
}

export function dispose(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    try { chartData.canvasRef.removeEventListener('click', chartData.onClick); } catch { }
    try { chartData.instance.destroy(); } catch { }
    chartInstances.delete(chartId);
}

export function getChartInstance(chartId) {
    return chartInstances.get(chartId)?.instance ?? null;
}
