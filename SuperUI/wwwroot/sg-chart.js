// SgChart - Chart.js Integration Module
// Provides JavaScript interop for Blazor SgChart component.

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

function readCssVar(name, fallback) {
    try {
        const v = getComputedStyle(document.documentElement).getPropertyValue(name);
        const trimmed = v && v.trim();
        return trimmed || fallback;
    } catch {
        return fallback;
    }
}

function applyThemeDefaults(Chart) {
    if (Chart.__sgThemed) return;
    Chart.__sgThemed = true;

    const text = readCssVar('--sui-text', '#1f2937');
    const muted = readCssVar('--sui-text-secondary', '#6b7280');
    const grid = readCssVar('--sui-border', 'rgba(127,127,127,0.18)');
    const cardBg = readCssVar('--sui-card-bg', '#ffffff');

    Chart.defaults.color = muted;
    Chart.defaults.borderColor = grid;
    Chart.defaults.font.family = readCssVar('--sui-font-family', Chart.defaults.font.family);
    Chart.defaults.plugins.tooltip.backgroundColor = readCssVar('--sui-tooltip-bg', 'rgba(17,24,39,0.92)');
    Chart.defaults.plugins.tooltip.titleColor = '#fff';
    Chart.defaults.plugins.tooltip.bodyColor = '#f3f4f6';
    Chart.defaults.plugins.tooltip.borderColor = 'transparent';
    Chart.defaults.plugins.title.color = text;
    Chart.defaults.plugins.legend.labels.color = text;
}

function applyOptionalPlugins(Chart, config) {
    if (window.ChartZoom) {
        try { Chart.register(window.ChartZoom); } catch { }
        config.options ??= {};
        config.options.plugins ??= {};
        config.options.plugins.zoom = config.options.plugins.zoom || {
            zoom: {
                wheel: { enabled: true, speed: 0.1, modifierKey: 'ctrl' },
                pinch: { enabled: true },
                mode: 'xy'
            },
            pan: { enabled: true, mode: 'xy', modifierKey: 'shift' }
        };
    }
    if (window.MatrixController) {
        try { Chart.register(window.MatrixController); } catch { }
    }
}

function installFormatters(config) {
    const opts = config.options;
    if (!opts) return;

    const tt = opts.plugins?.tooltip;
    const suffix = tt?.suffix ?? '';
    const decimals = tt?.decimals;

    const fmt = (v) => {
        if (v === null || v === undefined || Number.isNaN(v)) return '';
        if (typeof v !== 'number') return String(v);
        const fixed = (typeof decimals === 'number')
            ? v.toFixed(decimals)
            : v.toLocaleString(undefined, { maximumFractionDigits: 2 });
        return suffix ? `${fixed}${suffix}` : fixed;
    };

    if (tt) {
        tt.callbacks = tt.callbacks || {};
        tt.callbacks.label = tt.callbacks.label || function (ctx) {
            const dsLabel = ctx.dataset?.label || '';
            const raw = ctx.raw;
            const value = (typeof raw === 'object' && raw !== null) ? (raw.y ?? raw.v ?? 0) : raw;
            return dsLabel ? `${dsLabel}: ${fmt(value)}` : fmt(value);
        };
        delete tt.suffix;
        delete tt.decimals;
    }

    const yScale = opts.scales?.y;
    if (yScale && yScale.type === 'linear') {
        yScale.ticks = yScale.ticks || {};
        if (!yScale.ticks.callback) {
            yScale.ticks.callback = function (value) { return fmt(value); };
        }
    }
}

export async function initChart(dotnetRef, canvasRef, config) {
    const Chart = await waitForChart();
    applyThemeDefaults(Chart);
    applyOptionalPlugins(Chart, config);
    installFormatters(config);

    const ctx = canvasRef.getContext('2d');
    if (!ctx) throw new Error('Failed to get canvas context');

    const clickable = !!config.options?.__sgClickable;
    if (config.options) delete config.options.__sgClickable;

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

    const onMove = (e) => {
        const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
        canvasRef.style.cursor = points.length > 0 ? 'pointer' : 'default';
    };

    if (clickable) {
        canvasRef.addEventListener('click', onClick);
        canvasRef.addEventListener('mousemove', onMove);
    }

    let resizeObserver = null;
    const parent = canvasRef.parentElement;
    if (parent && typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        resizeObserver = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => {
                try { chart.resize(); } catch { }
            });
        });
        resizeObserver.observe(parent);
    }

    chartInstances.set(chartId, {
        instance: chart,
        dotnetRef,
        canvasRef,
        onClick: clickable ? onClick : null,
        onMove: clickable ? onMove : null,
        resizeObserver
    });
}

export function updateChart(chartId, config) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;

    installFormatters(config);
    if (config.options) delete config.options.__sgClickable;

    chart.data = config.data;
    chart.options = config.options;
    chart.update('none');
}

export function resizeChart(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    try { chartData.instance.resize(); } catch { }
}

export function zoomY(chartId, min, max) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;
    if (chart.options?.scales?.y) {
        chart.options.scales.y.min = min;
        chart.options.scales.y.max = max;
        chart.update('none');
    }
}

export function resetZoom(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) return;
    const chart = chartData.instance;
    if (chart.options?.scales?.y) {
        delete chart.options.scales.y.min;
        delete chart.options.scales.y.max;
    }
    if (chart.options?.scales?.x) {
        delete chart.options.scales.x.min;
        delete chart.options.scales.x.max;
    }
    if (typeof chart.resetZoom === 'function') {
        try { chart.resetZoom(); } catch { }
    }
    chart.update('none');
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
    try { chartData.resizeObserver?.disconnect(); } catch { }
    try {
        if (chartData.onClick) chartData.canvasRef.removeEventListener('click', chartData.onClick);
        if (chartData.onMove) chartData.canvasRef.removeEventListener('mousemove', chartData.onMove);
    } catch { }
    try { chartData.instance.destroy(); } catch { }
    chartInstances.delete(chartId);
}

export function getChartInstance(chartId) {
    return chartInstances.get(chartId)?.instance ?? null;
}
