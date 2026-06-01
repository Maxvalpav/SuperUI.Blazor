// SgChart - Chart.js Integration Module
// Provides JavaScript interop for Blazor SgChart component.

const chartInstances = new Map();

// ── Script / stylesheet loader ────────────────────────────────────────────────

const _loadedScripts = new Set();

function _loadScript(url, timeoutMs = 10000) {
    if (!url) return Promise.resolve();
    if (_loadedScripts.has(url)) return Promise.resolve();

    const existing = document.querySelector(`script[src="${url}"]`);
    if (existing) {
        // If script exists but we don't track it as loaded, it might be from index.html
        // or still loading. We'll assume it's loading or loaded.
        if (existing.dataset.loaded === 'true') {
            _loadedScripts.add(url);
            return Promise.resolve();
        }
        return new Promise((resolve, reject) => {
            const onOk = () => { resolve(); };
            const onErr = () => reject(new Error(`Script failed: ${url}`));
            existing.addEventListener('load', onOk, { once: true });
            existing.addEventListener('error', onErr, { once: true });
            // Fallback for already loaded scripts that didn't set data-loaded
            setTimeout(onOk, 2000); 
        });
    }

    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.async = true;
        
        const timeout = setTimeout(() => {
            script.onload = script.onerror = null;
            reject(new Error(`Timeout loading script: ${url}`));
        }, timeoutMs);

        script.onload = () => {
            clearTimeout(timeout);
            script.dataset.loaded = 'true';
            _loadedScripts.add(url);
            resolve();
        };
        script.onerror = () => {
            clearTimeout(timeout);
            reject(new Error(`Failed to load script: ${url}`));
        };
        document.head.appendChild(script);
    });
}

async function _ensureChart(sources) {
    console.log('[SgChart] Ensuring Chart.js and plugins...', sources);

    // 1. Load Chart.js first
    if (sources?.chartScript) {
        console.log('[SgChart] Loading Chart.js:', sources.chartScript);
        await _loadScript(sources.chartScript);
    }

    // 2. Wait for window.Chart to be available (mandatory for plugins)
    let Chart = window.Chart;
    let attempts = 0;
    while (!Chart && attempts < 60) {
        if (attempts % 10 === 0) console.log('[SgChart] Waiting for window.Chart...', attempts);
        await new Promise(r => setTimeout(r, 100));
        Chart = window.Chart;
        attempts++;
    }
    
    if (!Chart) {
        console.error('[SgChart] Chart.js NOT found after 6s');
        throw new Error('Chart.js library not loaded');
    }

    // 3. Load optional plugins ONLY after Chart.js is ready
    const pluginLoads = [];
    if (sources?.zoomScript)   {
        console.log('[SgChart] Loading Zoom plugin:', sources.zoomScript);
        pluginLoads.push(_loadScript(sources.zoomScript).catch(e => console.warn(e)));
    }
    if (sources?.matrixScript) {
        console.log('[SgChart] Loading Matrix plugin:', sources.matrixScript);
        pluginLoads.push(_loadScript(sources.matrixScript).catch(e => console.warn(e)));
    }
    if (sources?.dataLabelsScript) {
        console.log('[SgChart] Loading DataLabels plugin:', sources.dataLabelsScript);
        pluginLoads.push(_loadScript(sources.dataLabelsScript).catch(e => console.warn(e)));
    }
    
    if (pluginLoads.length) {
        await Promise.all(pluginLoads);
        console.log('[SgChart] Plugins loaded');
    }

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

    const text = readCssVar('--sg-fg', '#1f2937');
    const muted = readCssVar('--sg-fg-subtle', '#6b7280');
    const grid = readCssVar('--sg-border', 'rgba(127,127,127,0.18)');
    const cardBg = readCssVar('--sgc-card-bg', '#ffffff');

    Chart.defaults.color = muted;
    Chart.defaults.borderColor = grid;
    Chart.defaults.font.family = readCssVar('--sg-font', Chart.defaults.font.family);
    Chart.defaults.plugins.tooltip.backgroundColor = readCssVar('--sg-bg-muted', 'rgba(17,24,39,0.92)');
    Chart.defaults.plugins.tooltip.titleColor = '#fff';
    Chart.defaults.plugins.tooltip.bodyColor = '#f3f4f6';
    Chart.defaults.plugins.tooltip.borderColor = 'transparent';
    Chart.defaults.plugins.title.color = text;
    Chart.defaults.plugins.legend.labels.color = text;
}

function applyOptionalPlugins(Chart, config) {
    const sgOpts = config.options?.__sgOptions || {};
    if (config.options) delete config.options.__sgOptions;

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
    if (window.ChartDataLabels) {
        try { Chart.register(window.ChartDataLabels); } catch { }
        config.options ??= {};
        config.options.plugins ??= {};

        const enabled = !!sgOpts.showDataLabels;
        const decimals = sgOpts.dataLabelDecimals ?? 2;
        const suffix   = sgOpts.dataLabelSuffix   ?? '';
        const step     = sgOpts.dataLabelStep      ?? 0; // 0 = auto

        config.options.plugins.datalabels = {
            display: function(ctx) {
                if (!enabled) return false;
                // Auto-thin: show every N-th point to avoid overlap
                const total = ctx.dataset?.data?.length ?? 0;
                const autoStep = step > 0 ? step
                    : total > 60 ? 10
                    : total > 30 ? 5
                    : total > 15 ? 3
                    : total > 8  ? 2
                    : 1;
                return ctx.dataIndex % autoStep === 0;
            },
            align: 'top',
            anchor: 'end',
            offset: 6,
            font: { weight: '600', size: 11, family: readCssVar('--sg-font', 'system-ui') },
            color: function(ctx) {
                // Use dataset border color for matching label color
                return ctx.dataset?.borderColor ?? ctx.dataset?.backgroundColor ?? '#374151';
            },
            backgroundColor: function(ctx) {
                const bg = readCssVar('--sg-bg', '#ffffff');
                return bg + 'cc'; // 80% opacity
            },
            borderRadius: 3,
            padding: { top: 2, bottom: 2, left: 4, right: 4 },
            formatter: (v) => {
                if (v === null || v === undefined) return '';
                const val = (typeof v === 'object') ? (v.y ?? v.v ?? 0) : v;
                if (typeof val !== 'number') return String(val);
                const formatted = val.toLocaleString('ru-RU', {
                    minimumFractionDigits: decimals,
                    maximumFractionDigits: decimals
                });
                return suffix ? `${formatted}${suffix}` : formatted;
            }
        };
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

export async function initChart(dotnetRef, canvasRef, config, sources) {
    const Chart = await _ensureChart(sources);
    applyThemeDefaults(Chart);
    applyOptionalPlugins(Chart, config);
    installFormatters(config);

    const ctx = canvasRef.getContext('2d');
    if (!ctx) throw new Error('Failed to get canvas context');

    const clickable = !!config.options?.__sgClickable;
    if (config.options) delete config.options.__sgClickable;

    // Chart.js v4 бросает "Canvas is already in use" при создании нового графика
    // на canvas, к которому уже привязан chart. Это случается при:
    //   1) повторном rendering canvas с тем же id (например, после переключения SgTabs)
    //   2) ручном Retry
    //   3) race между init и dispose
    // Поэтому перед созданием явно убиваем предыдущий instance — и из нашего кеша,
    // и из глобального реестра Chart.js (на случай если кеш не синхронен с DOM).
    const chartId = canvasRef.id;
    const existing = chartInstances.get(chartId);
    if (existing) {
        try { existing.instance?.destroy(); } catch { }
        chartInstances.delete(chartId);
    }
    const stray = Chart.getChart?.(canvasRef);
    if (stray) {
        try { stray.destroy(); } catch { }
    }

    const chart = new Chart(ctx, config);

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
            })?.catch(() => {});
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

export async function updateChart(chartId, config) {
    try {
        const Chart = window.Chart;
        if (!Chart) return false;

        const chartData = chartInstances.get(chartId);
        // Возвращаем false, чтобы .NET-сторона могла сделать полный re-init вместо тихого
        // отвала (типичный кейс — переключение SgTabs пересоздало DOM и старый instance
        // потерян).
        if (!chartData) return false;

        installFormatters(config);
        const { instance: chart, canvasRef, dotnetRef, onClick, onMove, resizeObserver } = chartData;

        const clickable = !!config.options?.__sgClickable;
        if (config.options) delete config.options.__sgClickable;

        // If chart type is the same, we can update data smoothly.
        // In Chart.js v4, it's best to update properties on chart.data directly.
        if (chart.config.type === config.type) {
            chart.data.labels = config.data.labels;
            chart.data.datasets = config.data.datasets;

            // Update options - merging into config.options is safer in v4
            if (config.options) {
                if (Chart.helpers && typeof Chart.helpers.merge === 'function') {
                    // Update the configuration options; Chart.js will resolve them during update()
                    Chart.helpers.merge(chart.config.options, [config.options]);
                } else {
                    chart.config.options = { ...chart.config.options, ...config.options };
                }
            }

            chart.update(config.options?.animation?.duration === 0 ? 'none' : 'default');

            // Update stored handlers if needed
            chartData.onClick = clickable ? chartData.onClick : null;
            chartData.onMove = clickable ? chartData.onMove : null;
            return true;
        }

        // If type or scales changed significantly, recreation is safer.
        try { resizeObserver?.disconnect(); } catch {}
        try {
            canvasRef.removeEventListener('click', onClick);
            canvasRef.removeEventListener('mousemove', onMove);
            chart.destroy();
        } catch {}

        applyOptionalPlugins(Chart, config);

        const ctx = canvasRef.getContext('2d');
        const newChart = new Chart(ctx, config);

        const newOnClick = clickable ? (e) => {
            const points = newChart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
            if (points.length === 0) return;
            const point = points[0];
            const datasetIndex = point.datasetIndex;
            const index = point.index;
            const ds = newChart.data.datasets[datasetIndex];
            const raw = ds?.data?.[index];
            const value = typeof raw === 'object' && raw !== null ? (raw.y ?? raw.v ?? 0) : (raw ?? 0);
            const label = newChart.data.labels?.[index] ?? '';
            try {
                dotnetRef.invokeMethodAsync('OnDataPointClickedAsync', {
                    datasetIndex,
                    dataPointIndex: index,
                    value,
                    label
                })?.catch(() => {});
            } catch { }
        } : null;

        const newOnMove = clickable ? (e) => {
            const points = newChart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
            canvasRef.style.cursor = points.length > 0 ? 'pointer' : 'default';
        } : null;

        if (clickable) {
            canvasRef.addEventListener('click', newOnClick);
            canvasRef.addEventListener('mousemove', newOnMove);
        }

        let newResizeObs = null;
        const parent = canvasRef.parentElement;
        if (parent && typeof ResizeObserver !== 'undefined') {
            let raf = 0;
            newResizeObs = new ResizeObserver(() => {
                cancelAnimationFrame(raf);
                raf = requestAnimationFrame(() => { try { newChart.resize(); } catch {} });
            });
            newResizeObs.observe(parent);
        }

        chartInstances.set(chartId, {
            instance: newChart,
            dotnetRef,
            canvasRef,
            onClick: newOnClick,
            onMove: newOnMove,
            resizeObserver: newResizeObs,
        });
        return true;
    } catch (err) {
        console.error('[SgChart] updateChart failed for ' + chartId + ':', err);
        return false;
    }
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
