// SgECharts - Apache ECharts Integration Module for SuperUI Blazor

const _instances = new Map();
const _loaded    = new Set();

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url || _loaded.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) { _loaded.add(url); resolve(); return; }
        const s = document.createElement('script');
        s.src = url;
        s.onload  = () => { _loaded.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

async function _ensureECharts(sources) {
    if (sources?.eChartsScript) await _loadScript(sources.eChartsScript);
    let ec = window.echarts;
    let n = 0;
    while (!ec && n++ < 80) { await new Promise(r => setTimeout(r, 100)); ec = window.echarts; }
    if (!ec) throw new Error('ECharts not loaded');
    return ec;
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

function _theme() {
    return {
        text:    _cssVar('--sg-fg',   '#1e293b'),
        muted:   _cssVar('--sg-fg-subtle', '#64748b'),
        border:  _cssVar('--sg-border',         '#e2e8f0'),
        bg:      _cssVar('--sgc-card-bg',        '#ffffff'),
        accent:  _cssVar('--sg-color-primary',         '#006fee'),
    };
}

// ── Format helper ─────────────────────────────────────────────────────────────

function _fmt(v, opts) {
    if (v === null || v === undefined) return '';
    const dec = opts.valueDecimals;
    const s = typeof dec === 'number' ? Number(v).toFixed(dec) : Number(v).toLocaleString(undefined, { maximumFractionDigits: 2 });
    return opts.valueSuffix ? s + opts.valueSuffix : s;
}

// ── Option builders ───────────────────────────────────────────────────────────

function _baseOption(opts, t) {
    const base = {
        animation: opts.animate !== false,
        animationDuration: opts.animationDuration ?? 800,
        backgroundColor: opts.backgroundColor ?? 'transparent',
        color: opts.colors ?? ['#006fee','#10b981','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#84cc16','#ec4899','#f97316','#0ea5e9'],
        textStyle: { color: t.text, fontFamily: _cssVar('--sg-font', 'system-ui') },
        tooltip: {
            trigger: 'axis',
            backgroundColor: t.bg,
            borderColor: t.border,
            textStyle: { color: t.text },
            formatter: opts.valueSuffix || opts.valueDecimals != null
                ? (params) => {
                    const lines = Array.isArray(params) ? params : [params];
                    const title = lines[0]?.axisValueLabel ?? lines[0]?.name ?? '';
                    const body  = lines.map(p => `${p.marker}${p.seriesName}: <b>${_fmt(p.value, opts)}</b>`).join('<br/>');
                    return title ? `${title}<br/>${body}` : body;
                }
                : undefined,
        },
        legend: opts.showLegend !== false ? { textStyle: { color: t.muted }, top: 4 } : { show: false },
    };
    if (opts.showToolbar !== false) {
        base.toolbox = {
            feature: {
                saveAsImage: { title: 'Save' },
                dataZoom:    opts.enableZoom ? { title: { zoom: 'Zoom', back: 'Reset' } } : undefined,
                restore:     { title: 'Reset' },
            },
            iconStyle: { borderColor: t.muted },
        };
    }
    return base;
}

function _axisBase(t, opts) {
    return {
        axisLine:  { lineStyle: { color: t.border } },
        axisTick:  { lineStyle: { color: t.border } },
        axisLabel: { color: t.muted, show: opts.showLabels !== false },
        splitLine: { lineStyle: { color: t.border }, show: opts.showGrid !== false },
    };
}

function _buildOption(chartType, data, graphData, candleData, opts) {
    const t = _theme();
    const base = _baseOption(opts, t);
    const ax   = _axisBase(t, opts);

    const groups = [...new Set((data ?? []).map(d => d.group || 'default'))];
    const labels = [...new Set((data ?? []).map(d => d.label))];

    const dataZoom = opts.enableZoom ? [
        { type: 'inside', xAxisIndex: 0 },
        { type: 'slider', xAxisIndex: 0, bottom: 4, height: 20, borderColor: t.border, fillerColor: 'rgba(0,111,238,0.1)', handleStyle: { color: t.accent } },
    ] : undefined;

    switch (chartType) {

        case 'Line':
        case 'Area': {
            const isArea = chartType === 'Area';
            return {
                ...base,
                grid: { left: 50, right: 20, top: 40, bottom: dataZoom ? 50 : 30 },
                xAxis: { type: 'category', data: labels, ...ax },
                yAxis: { type: 'value', ...ax },
                dataZoom,
                series: groups.map((grp, gi) => ({
                    name: grp === 'default' ? (opts.seriesName ?? 'Series') : grp,
                    type: 'line',
                    smooth: opts.smooth ?? false,
                    symbol: opts.showPoints !== false ? 'circle' : 'none',
                    symbolSize: 5,
                    areaStyle: isArea ? { opacity: 0.25 } : undefined,
                    stack: opts.stacked ? 'total' : undefined,
                    data: labels.map(l => {
                        const d = (data ?? []).find(x => x.label === l && (x.group || 'default') === grp);
                        return d ? d.value : null;
                    }),
                })),
            };
        }

        case 'Bar':
        case 'BarHorizontal': {
            const horiz = chartType === 'BarHorizontal';
            const catAxis = { type: 'category', data: labels, ...ax };
            const valAxis = { type: 'value', ...ax };
            return {
                ...base,
                grid: { left: horiz ? 100 : 50, right: 20, top: 40, bottom: dataZoom ? 50 : 30 },
                xAxis: horiz ? valAxis : catAxis,
                yAxis: horiz ? catAxis : valAxis,
                dataZoom,
                series: groups.map((grp, gi) => ({
                    name: grp === 'default' ? (opts.seriesName ?? 'Series') : grp,
                    type: 'bar',
                    stack: opts.stacked ? 'total' : undefined,
                    barMaxWidth: 40,
                    itemStyle: { borderRadius: horiz ? [0,3,3,0] : [3,3,0,0] },
                    data: labels.map(l => {
                        const d = (data ?? []).find(x => x.label === l && (x.group || 'default') === grp);
                        return d ? d.value : null;
                    }),
                })),
            };
        }

        case 'Pie':
        case 'Donut': {
            const inner = chartType === 'Donut' ? `${Math.round((opts.donutInnerRadius ?? 0.5) * 100)}%` : '0%';
            return {
                ...base,
                tooltip: { ...base.tooltip, trigger: 'item',
                    formatter: p => `${p.name}: <b>${_fmt(p.value, opts)}</b> (${p.percent}%)` },
                series: [{
                    type: 'pie',
                    radius: [inner, '70%'],
                    center: ['50%', '55%'],
                    label: { color: t.muted, fontSize: 11 },
                    data: (data ?? []).map(d => ({ name: d.label, value: d.value, itemStyle: d.color ? { color: d.color } : undefined })),
                }],
            };
        }

        case 'Scatter': {
            return {
                ...base,
                grid: { left: 50, right: 20, top: 40, bottom: 30 },
                xAxis: { type: 'value', ...ax },
                yAxis: { type: 'value', ...ax },
                tooltip: { ...base.tooltip, trigger: 'item', formatter: p => `${p.seriesName}<br/>${p.data[0]}, ${_fmt(p.data[1], opts)}` },
                series: groups.map((grp, gi) => ({
                    name: grp === 'default' ? 'Data' : grp,
                    type: 'scatter',
                    symbolSize: 8,
                    data: (data ?? []).filter(d => (d.group || 'default') === grp).map((d, i) => [i, d.value]),
                })),
            };
        }

        case 'Radar': {
            const axes = [...new Set((data ?? []).map(d => d.label))];
            const maxVal = Math.max(...(data ?? []).map(d => d.value), 1);
            return {
                ...base,
                radar: {
                    indicator: axes.map(a => ({ name: a, max: maxVal * 1.1 })),
                    axisLine:  { lineStyle: { color: t.border } },
                    splitLine: { lineStyle: { color: t.border } },
                    name:      { textStyle: { color: t.muted } },
                },
                series: [{
                    type: 'radar',
                    data: groups.map(grp => ({
                        name: grp === 'default' ? 'Data' : grp,
                        value: axes.map(a => {
                            const d = (data ?? []).find(x => x.label === a && (x.group || 'default') === grp);
                            return d ? d.value : 0;
                        }),
                        areaStyle: { opacity: 0.15 },
                    })),
                }],
            };
        }

        case 'Heatmap': {
            const xLabels = [...new Set((data ?? []).map(d => d.label))];
            const yLabels = [...new Set((data ?? []).map(d => d.group ?? ''))];
            const vals    = (data ?? []).map(d => d.value);
            const minV = Math.min(...vals), maxV = Math.max(...vals);
            return {
                ...base,
                grid: { left: 80, right: 60, top: 40, bottom: 40 },
                xAxis: { type: 'category', data: xLabels, ...ax },
                yAxis: { type: 'category', data: yLabels, ...ax },
                visualMap: { min: minV, max: maxV, calculable: true, orient: 'horizontal', left: 'center', bottom: 0,
                    inRange: { color: ['#dbeafe','#1d4ed8'] }, textStyle: { color: t.muted } },
                series: [{
                    type: 'heatmap',
                    data: (data ?? []).map(d => [xLabels.indexOf(d.label), yLabels.indexOf(d.group ?? ''), d.value]),
                    label: { show: true, color: t.text, fontSize: 10 },
                }],
            };
        }

        case 'Gauge': {
            const val = (data ?? [])[0]?.value ?? 0;
            const min = opts.gaugeMin ?? 0, max = opts.gaugeMax ?? 100;
            return {
                ...base,
                series: [{
                    type: 'gauge',
                    min, max,
                    progress: { show: true, width: 14 },
                    axisLine: { lineStyle: { width: 14, color: [[1, t.border]] } },
                    axisTick: { show: false },
                    splitLine: { length: 10, lineStyle: { color: t.muted } },
                    axisLabel: { color: t.muted, distance: 20 },
                    pointer: { itemStyle: { color: t.accent } },
                    detail: { valueAnimation: true, formatter: v => _fmt(v, opts), color: t.text, fontSize: 22, fontWeight: 'bold' },
                    title: { color: t.muted, fontSize: 12 },
                    data: [{ value: val, name: (data ?? [])[0]?.label ?? '' }],
                }],
            };
        }

        case 'Funnel': {
            return {
                ...base,
                tooltip: { ...base.tooltip, trigger: 'item', formatter: p => `${p.name}: <b>${_fmt(p.value, opts)}</b>` },
                series: [{
                    type: 'funnel',
                    left: '10%', width: '80%',
                    label: { position: 'inside', color: '#fff', fontWeight: 'bold' },
                    data: (data ?? []).map(d => ({ name: d.label, value: d.value })),
                }],
            };
        }

        case 'Sankey': {
            return {
                ...base,
                tooltip: { ...base.tooltip, trigger: 'item', formatter: p => `${p.name}: <b>${_fmt(p.value, opts)}</b>` },
                series: [{
                    type: 'sankey',
                    layout: 'none',
                    emphasis: { focus: 'adjacency' },
                    label: { color: t.text },
                    lineStyle: { color: 'gradient', opacity: 0.4 },
                    data: (graphData?.nodes ?? []).map(n => ({ name: n.name, value: n.value })),
                    links: (graphData?.links ?? []).map(l => ({ source: l.source, target: l.target, value: l.value })),
                }],
            };
        }

        case 'Tree': {
            return {
                ...base,
                tooltip: { ...base.tooltip, trigger: 'item' },
                series: [{
                    type: 'tree',
                    data: graphData?.nodes?.length ? [_toTreeNode(graphData.nodes[0])] : [],
                    top: '5%', left: '10%', bottom: '5%', right: '20%',
                    symbolSize: 8,
                    label: { position: 'left', verticalAlign: 'middle', align: 'right', color: t.text, fontSize: 11 },
                    leaves: { label: { position: 'right', verticalAlign: 'middle', align: 'left' } },
                    emphasis: { focus: 'descendant' },
                    expandAndCollapse: true,
                }],
            };
        }

        case 'Sunburst': {
            return {
                ...base,
                tooltip: { ...base.tooltip, trigger: 'item' },
                series: [{
                    type: 'sunburst',
                    data: (graphData?.nodes ?? []).map(n => _toSunburstNode(n)),
                    radius: ['15%', '80%'],
                    label: { color: t.text, fontSize: 10 },
                    emphasis: { focus: 'ancestor' },
                }],
            };
        }

        case 'Candlestick': {
            const cdl = candleData ?? [];
            return {
                ...base,
                grid: { left: 60, right: 20, top: 40, bottom: dataZoom ? 60 : 30 },
                xAxis: { type: 'category', data: cdl.map(c => c.date), ...ax, scale: true },
                yAxis: { type: 'value', ...ax, scale: true },
                dataZoom,
                tooltip: { ...base.tooltip, trigger: 'axis', axisPointer: { type: 'cross' },
                    formatter: params => {
                        const p = params[0]; if (!p) return '';
                        const [o,c,l,h] = p.data;
                        return `${p.axisValue}<br/>O: ${o}  C: ${c}<br/>L: ${l}  H: ${h}`;
                    }
                },
                series: [{
                    type: 'candlestick',
                    data: cdl.map(c => [c.open, c.close, c.low, c.high]),
                    itemStyle: {
                        color: '#22c55e', color0: '#ef4444',
                        borderColor: '#16a34a', borderColor0: '#dc2626',
                    },
                }],
            };
        }

        default: return { ...base };
    }
}

function _toTreeNode(n) {
    return { name: n.name, value: n.value, children: (n.children ?? []).map(_toTreeNode) };
}
function _toSunburstNode(n) {
    return { name: n.name, value: n.value, children: (n.children ?? []).map(_toSunburstNode) };
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initECharts(dotnetRef, containerRef, instanceId, chartType, data, graphData, candleData, opts, sources) {
    await disposeECharts(instanceId);

    const ec = await _ensureECharts(sources);
    const chart = ec.init(containerRef, null, { renderer: 'canvas' });

    const option = _buildOption(chartType, data, graphData, candleData, opts ?? {});
    chart.setOption(option);

    chart.on('click', (params) => {
        try {
            dotnetRef.invokeMethodAsync('OnDataPointClickedAsync', {
                seriesName:  String(params.seriesName ?? ''),
                name:        String(params.name ?? ''),
                value:       Number(params.value ?? 0),
                dataIndex:   params.dataIndex ?? 0,
                seriesIndex: params.seriesIndex ?? 0,
            });
        } catch {}
    });

    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { try { chart.resize(); } catch {} });
        });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { chart, ro, ec });
}

export async function updateECharts(instanceId, chartType, data, graphData, candleData, opts) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const option = _buildOption(chartType, data, graphData, candleData, opts ?? {});
    inst.chart.setOption(option, { notMerge: true });
}

export function resizeECharts(instanceId) {
    const inst = _instances.get(instanceId);
    if (inst) try { inst.chart.resize(); } catch {}
}

export function exportImage(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const url = inst.chart.getDataURL({ type: 'png', pixelRatio: 2, backgroundColor: '#fff' });
    const a = document.createElement('a');
    a.href = url; a.download = `chart-${Date.now()}.png`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

export async function disposeECharts(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.chart.dispose(); } catch {}
    _instances.delete(instanceId);
}
