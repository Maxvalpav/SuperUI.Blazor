// SgD3 - D3.js Integration Module for SuperUI Blazor
// Provides JS interop for SgD3Chart component.

const _instances = new Map();
const _loadedScripts = new Set();

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url) return Promise.resolve();
    if (_loadedScripts.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) {
            _loadedScripts.add(url); resolve(); return;
        }
        const s = document.createElement('script');
        s.src = url;
        s.onload = () => { _loadedScripts.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

async function _ensureD3(sources) {
    if (sources && sources.d3Script) await _loadScript(sources.d3Script);
    let d3 = window.d3;
    let attempts = 0;
    while (!d3 && attempts < 50) {
        await new Promise(r => setTimeout(r, 100));
        d3 = window.d3; attempts++;
    }
    if (!d3) throw new Error('D3.js not loaded');
    return d3;
}

// ── Theme helpers ─────────────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try {
        const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        return v || fallback;
    } catch { return fallback; }
}

function _theme() {
    return {
        text:       _cssVar('--sg-fg',           '#1f2937'),
        textMuted:  _cssVar('--sg-fg-subtle', '#6b7280'),
        border:     _cssVar('--sg-border',         'rgba(127,127,127,0.18)'),
        bg:         _cssVar('--sgc-card-bg',        '#ffffff'),
        accent:     _cssVar('--sg-color-primary',         '#2563eb'),
        font:       _cssVar('--sg-font',    'system-ui,sans-serif'),
        palette: [
            _cssVar('--sg-color-primary',  '#2563eb'),
            '#10b981','#f59e0b','#ef4444','#8b5cf6',
            '#06b6d4','#84cc16','#ec4899','#f97316','#0ea5e9'
        ]
    };
}

// ── Tooltip ───────────────────────────────────────────────────────────────────

function _makeTooltip(container) {
    let tip = container.select('.sg-d3-tooltip');
    if (tip.empty()) {
        tip = container.append('div').attr('class', 'sg-d3-tooltip');
    }
    return tip;
}

function _showTip(tip, event, html) {
    tip.style('opacity', '1').html(html);
    const rect = tip.node().parentElement.getBoundingClientRect();
    const x = event.clientX - rect.left + 12;
    const y = event.clientY - rect.top - 28;
    tip.style('left', x + 'px').style('top', y + 'px');
}

function _hideTip(tip) { tip.style('opacity', '0'); }

function _fmt(v, opts) {
    if (v === null || v === undefined) return '';
    const dec = opts.valueDecimals;
    const s = typeof dec === 'number' ? v.toFixed(dec) : v.toLocaleString(undefined, { maximumFractionDigits: 2 });
    return opts.valueSuffix ? s + opts.valueSuffix : s;
}

// ── Resize observer ───────────────────────────────────────────────────────────

function _attachResize(containerId, el, redrawFn) {
    if (typeof ResizeObserver === 'undefined') return null;
    let raf = 0;
    const ro = new ResizeObserver(() => {
        cancelAnimationFrame(raf);
        raf = requestAnimationFrame(() => { try { redrawFn(); } catch {} });
    });
    ro.observe(el);
    return ro;
}


// ── Bar chart ─────────────────────────────────────────────────────────────────

function _drawBar(d3, svg, data, opts, width, height, horizontal, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const margin = { top: 20, right: 20, bottom: horizontal ? 20 : 40, left: horizontal ? 120 : 50 };
    const W = width  - margin.left - margin.right;
    const H = height - margin.top  - margin.bottom;

    svg.selectAll('*').remove();
    const g = svg.append('g').attr('transform', `translate(${margin.left},${margin.top})`);

    // Group by group field for stacked / multi-series
    const groups = [...new Set(data.map(d => d.group || 'default'))];
    const labels = [...new Set(data.map(d => d.label))];
    const isMulti = groups.length > 1 || opts.stacked;

    const tip = _makeTooltip(d3.select(svg.node().parentElement));

    if (horizontal) {
        const y0 = d3.scaleBand().domain(labels).range([0, H]).padding(0.25);
        const y1 = isMulti && !opts.stacked ? d3.scaleBand().domain(groups).range([0, y0.bandwidth()]).padding(0.05) : null;
        const maxVal = opts.stacked
            ? d3.max(labels, l => d3.sum(data.filter(d => d.label === l), d => d.value))
            : d3.max(data, d => d.value);
        const x = d3.scaleLinear().domain([0, maxVal * 1.05]).range([0, W]);

        if (opts.showGrid) {
            g.append('g').attr('class', 'sg-d3-grid')
                .call(d3.axisBottom(x).tickSize(H).tickFormat(''))
                .call(gg => { gg.select('.domain').remove(); gg.selectAll('line').attr('stroke', t.border); });
        }
        g.append('g').call(d3.axisLeft(y0).tickSize(0)).call(gg => gg.select('.domain').remove())
            .selectAll('text').style('fill', t.textMuted).style('font-size', '12px');
        g.append('g').attr('transform', `translate(0,${H})`).call(d3.axisBottom(x).ticks(5))
            .call(gg => gg.select('.domain').remove())
            .selectAll('text').style('fill', t.textMuted).style('font-size', '11px');

        labels.forEach(label => {
            const labelData = data.filter(d => d.label === label);
            if (opts.stacked) {
                let offset = 0;
                labelData.forEach((d, i) => {
                    const ci = groups.indexOf(d.group || 'default');
                    g.append('rect')
                        .attr('y', y0(label)).attr('x', x(offset))
                        .attr('height', y0.bandwidth()).attr('width', opts.animate ? 0 : x(d.value))
                        .attr('fill', colors[ci % colors.length]).attr('rx', 3)
                        .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong><br/>${d.group || ''}: ${_fmt(d.value, opts)}`))
                        .on('mouseout', () => _hideTip(tip))
                        .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: i }))
                        .transition().duration(opts.animate ? opts.animationDuration : 0).attr('width', x(d.value));
                    offset += d.value;
                });
            } else if (y1) {
                labelData.forEach((d, i) => {
                    const ci = groups.indexOf(d.group || 'default');
                    g.append('rect')
                        .attr('y', y0(label) + y1(d.group || 'default')).attr('x', 0)
                        .attr('height', y1.bandwidth()).attr('width', opts.animate ? 0 : x(d.value))
                        .attr('fill', colors[ci % colors.length]).attr('rx', 2)
                        .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong><br/>${d.group || ''}: ${_fmt(d.value, opts)}`))
                        .on('mouseout', () => _hideTip(tip))
                        .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: i }))
                        .transition().duration(opts.animate ? opts.animationDuration : 0).attr('width', x(d.value));
                });
            } else {
                const d = labelData[0];
                g.append('rect')
                    .attr('y', y0(label)).attr('x', 0)
                    .attr('height', y0.bandwidth()).attr('width', opts.animate ? 0 : x(d.value))
                    .attr('fill', colors[0]).attr('rx', 3)
                    .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong>: ${_fmt(d.value, opts)}`))
                    .on('mouseout', () => _hideTip(tip))
                    .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: null, index: 0 }))
                    .transition().duration(opts.animate ? opts.animationDuration : 0).attr('width', x(d.value));
            }
        });
    } else {
        // Vertical bar
        const x0 = d3.scaleBand().domain(labels).range([0, W]).padding(0.25);
        const x1 = isMulti && !opts.stacked ? d3.scaleBand().domain(groups).range([0, x0.bandwidth()]).padding(0.05) : null;
        const maxVal = opts.stacked
            ? d3.max(labels, l => d3.sum(data.filter(d => d.label === l), d => d.value))
            : d3.max(data, d => d.value);
        const y = d3.scaleLinear().domain([0, maxVal * 1.05]).range([H, 0]);

        if (opts.showGrid) {
            g.append('g').attr('class', 'sg-d3-grid')
                .call(d3.axisLeft(y).tickSize(-W).tickFormat(''))
                .call(gg => { gg.select('.domain').remove(); gg.selectAll('line').attr('stroke', t.border); });
        }
        g.append('g').attr('transform', `translate(0,${H})`).call(d3.axisBottom(x0).tickSize(0))
            .call(gg => gg.select('.domain').remove())
            .selectAll('text').style('fill', t.textMuted).style('font-size', '12px').attr('dy', '1em');
        g.append('g').call(d3.axisLeft(y).ticks(5))
            .call(gg => gg.select('.domain').remove())
            .selectAll('text').style('fill', t.textMuted).style('font-size', '11px');

        labels.forEach(label => {
            const labelData = data.filter(d => d.label === label);
            if (opts.stacked) {
                let offset = 0;
                labelData.forEach((d, i) => {
                    const ci = groups.indexOf(d.group || 'default');
                    const barH = H - y(d.value);
                    g.append('rect')
                        .attr('x', x0(label)).attr('y', opts.animate ? H : y(offset + d.value))
                        .attr('width', x0.bandwidth()).attr('height', opts.animate ? 0 : barH)
                        .attr('fill', colors[ci % colors.length]).attr('rx', 3)
                        .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong><br/>${d.group || ''}: ${_fmt(d.value, opts)}`))
                        .on('mouseout', () => _hideTip(tip))
                        .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: i }))
                        .transition().duration(opts.animate ? opts.animationDuration : 0)
                        .attr('y', y(offset + d.value)).attr('height', barH);
                    offset += d.value;
                });
            } else if (x1) {
                labelData.forEach((d, i) => {
                    const ci = groups.indexOf(d.group || 'default');
                    const barH = H - y(d.value);
                    g.append('rect')
                        .attr('x', x0(label) + x1(d.group || 'default')).attr('y', opts.animate ? H : y(d.value))
                        .attr('width', x1.bandwidth()).attr('height', opts.animate ? 0 : barH)
                        .attr('fill', colors[ci % colors.length]).attr('rx', 2)
                        .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong><br/>${d.group || ''}: ${_fmt(d.value, opts)}`))
                        .on('mouseout', () => _hideTip(tip))
                        .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: i }))
                        .transition().duration(opts.animate ? opts.animationDuration : 0)
                        .attr('y', y(d.value)).attr('height', barH);
                });
            } else {
                const d = labelData[0];
                const barH = H - y(d.value);
                g.append('rect')
                    .attr('x', x0(label)).attr('y', opts.animate ? H : y(d.value))
                    .attr('width', x0.bandwidth()).attr('height', opts.animate ? 0 : barH)
                    .attr('fill', colors[0]).attr('rx', 3)
                    .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${d.label}</strong>: ${_fmt(d.value, opts)}`))
                    .on('mouseout', () => _hideTip(tip))
                    .on('click', () => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: null, index: 0 }))
                    .transition().duration(opts.animate ? opts.animationDuration : 0)
                    .attr('y', y(d.value)).attr('height', barH);
            }
        });
    }
}

// ── Line / Area chart ─────────────────────────────────────────────────────────

function _drawLine(d3, svg, data, opts, width, height, isArea, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const margin = { top: 20, right: 20, bottom: 40, left: 55 };
    const W = width  - margin.left - margin.right;
    const H = height - margin.top  - margin.bottom;

    svg.selectAll('*').remove();
    const g = svg.append('g').attr('transform', `translate(${margin.left},${margin.top})`);

    const groups = [...new Set(data.map(d => d.group || 'default'))];
    const labels = [...new Set(data.map(d => d.label))];

    const x = d3.scalePoint().domain(labels).range([0, W]).padding(0.1);
    const maxVal = d3.max(data, d => d.value) || 1;
    const y = d3.scaleLinear().domain([0, maxVal * 1.1]).range([H, 0]);

    const curveMap = {
        linear:   d3.curveLinear,
        monotone: d3.curveMonotoneX,
        step:     d3.curveStep,
        basis:    d3.curveBasis,
    };
    const curve = curveMap[opts.curve] || d3.curveMonotoneX;

    if (opts.showGrid) {
        g.append('g').attr('class', 'sg-d3-grid')
            .call(d3.axisLeft(y).tickSize(-W).tickFormat(''))
            .call(gg => { gg.select('.domain').remove(); gg.selectAll('line').attr('stroke', t.border); });
    }
    g.append('g').attr('transform', `translate(0,${H})`).call(d3.axisBottom(x).tickSize(0))
        .call(gg => gg.select('.domain').remove())
        .selectAll('text').style('fill', t.textMuted).style('font-size', '12px').attr('dy', '1em');
    g.append('g').call(d3.axisLeft(y).ticks(5))
        .call(gg => gg.select('.domain').remove())
        .selectAll('text').style('fill', t.textMuted).style('font-size', '11px');

    const tip = _makeTooltip(d3.select(svg.node().parentElement));

    groups.forEach((grp, gi) => {
        const color = colors[gi % colors.length];
        const grpData = data.filter(d => (d.group || 'default') === grp).sort((a, b) => labels.indexOf(a.label) - labels.indexOf(b.label));

        if (isArea) {
            const areaGen = d3.area()
                .x(d => x(d.label)).y0(H).y1(d => y(d.value)).curve(curve);
            const path = g.append('path').datum(grpData)
                .attr('fill', color).attr('opacity', 0.18)
                .attr('d', areaGen);
            if (opts.animate) {
                const len = path.node().getTotalLength();
                path.attr('stroke-dasharray', `${len} ${len}`).attr('stroke-dashoffset', len)
                    .transition().duration(opts.animationDuration).attr('stroke-dashoffset', 0);
            }
        }

        const lineGen = d3.line().x(d => x(d.label)).y(d => y(d.value)).curve(curve);
        const path = g.append('path').datum(grpData)
            .attr('fill', 'none').attr('stroke', color).attr('stroke-width', 2.5)
            .attr('d', lineGen);

        if (opts.animate) {
            const len = path.node().getTotalLength();
            path.attr('stroke-dasharray', `${len} ${len}`).attr('stroke-dashoffset', len)
                .transition().duration(opts.animationDuration).attr('stroke-dashoffset', 0);
        }

        if (opts.showPoints) {
            g.selectAll(`.dot-${gi}`).data(grpData).enter().append('circle')
                .attr('class', `dot-${gi}`)
                .attr('cx', d => x(d.label)).attr('cy', d => y(d.value))
                .attr('r', 4).attr('fill', color).attr('stroke', t.bg).attr('stroke-width', 2)
                .style('cursor', 'pointer')
                .on('mouseover', (ev, d) => _showTip(tip, ev, `<strong>${d.label}</strong>${grp !== 'default' ? '<br/>' + grp : ''}: ${_fmt(d.value, opts)}`))
                .on('mouseout', () => _hideTip(tip))
                .on('click', (ev, d) => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: grpData.indexOf(d) }));
        }
    });

    // Legend
    if (opts.showLegend && groups.length > 1) {
        _drawLegend(d3, g, groups, colors, W, -16);
    }
}

// ── Pie / Donut ───────────────────────────────────────────────────────────────

function _drawPie(d3, svg, data, opts, width, height, isDonut, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const cx = width / 2, cy = height / 2;
    const radius = Math.min(width, height) / 2 - 20;
    const inner = isDonut ? radius * (opts.donutInnerRadius || 0.55) : 0;

    svg.selectAll('*').remove();
    const g = svg.append('g').attr('transform', `translate(${cx},${cy})`);

    const pie = d3.pie().value(d => d.value).sort(null);
    const arc = d3.arc().innerRadius(inner).outerRadius(radius);
    const arcHover = d3.arc().innerRadius(inner).outerRadius(radius + 8);
    const tip = _makeTooltip(d3.select(svg.node().parentElement));
    const total = d3.sum(data, d => d.value);

    const arcs = g.selectAll('.arc').data(pie(data)).enter().append('g').attr('class', 'arc');

    arcs.append('path')
        .attr('fill', (d, i) => colors[i % colors.length])
        .attr('stroke', t.bg).attr('stroke-width', 2)
        .style('cursor', 'pointer')
        .on('mouseover', function(ev, d) {
            d3.select(this).transition().duration(150).attr('d', arcHover);
            const pct = total > 0 ? ((d.data.value / total) * 100).toFixed(1) : 0;
            _showTip(tip, ev, `<strong>${d.data.label}</strong><br/>${_fmt(d.data.value, opts)} (${pct}%)`);
        })
        .on('mouseout', function() {
            d3.select(this).transition().duration(150).attr('d', arc);
            _hideTip(tip);
        })
        .on('click', (ev, d) => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.data.label, value: d.data.value, group: null, index: d.index }))
        .transition().duration(opts.animate ? opts.animationDuration : 0)
        .attrTween('d', function(d) {
            const i = d3.interpolate({ startAngle: 0, endAngle: 0 }, d);
            return t => arc(i(t));
        });

    if (isDonut && data.length > 0) {
        g.append('text').attr('text-anchor', 'middle').attr('dy', '0.35em')
            .style('font-size', '18px').style('font-weight', '600').style('fill', t.text)
            .text(_fmt(total, opts));
    }

    if (opts.showLegend) {
        const legendG = svg.append('g').attr('transform', `translate(${cx - (data.length * 70) / 2},${height - 24})`);
        data.forEach((d, i) => {
            const lx = i * 70;
            legendG.append('rect').attr('x', lx).attr('y', 0).attr('width', 10).attr('height', 10)
                .attr('rx', 2).attr('fill', colors[i % colors.length]);
            legendG.append('text').attr('x', lx + 14).attr('y', 9)
                .style('font-size', '11px').style('fill', t.textMuted).text(d.label.length > 8 ? d.label.slice(0, 8) + '…' : d.label);
        });
    }
}

// ── Scatter ───────────────────────────────────────────────────────────────────

function _drawScatter(d3, svg, data, opts, width, height, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const margin = { top: 20, right: 20, bottom: 40, left: 55 };
    const W = width  - margin.left - margin.right;
    const H = height - margin.top  - margin.bottom;

    svg.selectAll('*').remove();
    const g = svg.append('g').attr('transform', `translate(${margin.left},${margin.top})`);

    const groups = [...new Set(data.map(d => d.group || 'default'))];
    const xVals = data.map((d, i) => d.x !== undefined ? d.x : i);
    const x = d3.scaleLinear().domain([d3.min(xVals) * 0.95, d3.max(xVals) * 1.05]).range([0, W]);
    const y = d3.scaleLinear().domain([0, d3.max(data, d => d.value) * 1.1]).range([H, 0]);

    if (opts.showGrid) {
        g.append('g').attr('class', 'sg-d3-grid')
            .call(d3.axisLeft(y).tickSize(-W).tickFormat(''))
            .call(gg => { gg.select('.domain').remove(); gg.selectAll('line').attr('stroke', t.border); });
        g.append('g').attr('class', 'sg-d3-grid')
            .call(d3.axisBottom(x).tickSize(H).tickFormat(''))
            .call(gg => { gg.select('.domain').remove(); gg.selectAll('line').attr('stroke', t.border); });
    }
    g.append('g').attr('transform', `translate(0,${H})`).call(d3.axisBottom(x).ticks(6))
        .call(gg => gg.select('.domain').remove()).selectAll('text').style('fill', t.textMuted).style('font-size', '11px');
    g.append('g').call(d3.axisLeft(y).ticks(5))
        .call(gg => gg.select('.domain').remove()).selectAll('text').style('fill', t.textMuted).style('font-size', '11px');

    const tip = _makeTooltip(d3.select(svg.node().parentElement));

    g.selectAll('.dot').data(data).enter().append('circle')
        .attr('class', 'dot')
        .attr('cx', (d, i) => x(d.x !== undefined ? d.x : i))
        .attr('cy', d => y(d.value))
        .attr('r', opts.animate ? 0 : 5)
        .attr('fill', d => colors[groups.indexOf(d.group || 'default') % colors.length])
        .attr('opacity', 0.8).attr('stroke', t.bg).attr('stroke-width', 1.5)
        .style('cursor', 'pointer')
        .on('mouseover', (ev, d) => _showTip(tip, ev, `<strong>${d.label}</strong>: ${_fmt(d.value, opts)}`))
        .on('mouseout', () => _hideTip(tip))
        .on('click', (ev, d) => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value, group: d.group, index: data.indexOf(d) }))
        .transition().duration(opts.animate ? opts.animationDuration : 0).attr('r', 5);

    if (opts.showLegend && groups.length > 1) _drawLegend(d3, g, groups, colors, W, -16);
}

// ── Force graph ───────────────────────────────────────────────────────────────

function _drawForce(d3, svg, graphData, opts, width, height, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const groups = [...new Set(graphData.nodes.map(n => n.group || 'default'))];

    svg.selectAll('*').remove();

    const sim = d3.forceSimulation(graphData.nodes)
        .force('link', d3.forceLink(graphData.links).id(d => d.id).distance(opts.forceDistance || 80))
        .force('charge', d3.forceManyBody().strength(opts.forceCharge || -200))
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('collision', d3.forceCollide(20));

    const link = svg.append('g').selectAll('line').data(graphData.links).enter().append('line')
        .attr('stroke', t.border).attr('stroke-width', d => Math.sqrt(d.value || 1) * 1.5).attr('stroke-opacity', 0.7);

    const node = svg.append('g').selectAll('g').data(graphData.nodes).enter().append('g')
        .style('cursor', 'pointer')
        .call(d3.drag()
            .on('start', (ev, d) => { if (!ev.active) sim.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y; })
            .on('drag',  (ev, d) => { d.fx = ev.x; d.fy = ev.y; })
            .on('end',   (ev, d) => { if (!ev.active) sim.alphaTarget(0); d.fx = null; d.fy = null; }));

    node.append('circle').attr('r', d => 8 + Math.sqrt(d.value || 1) * 2)
        .attr('fill', d => colors[groups.indexOf(d.group || 'default') % colors.length])
        .attr('stroke', t.bg).attr('stroke-width', 2);

    node.append('text').text(d => d.label).attr('dy', '0.35em').attr('dx', 12)
        .style('font-size', '11px').style('fill', t.text).style('pointer-events', 'none');

    const tip = _makeTooltip(d3.select(svg.node().parentElement));
    node.on('mouseover', (ev, d) => _showTip(tip, ev, `<strong>${d.label}</strong>${d.value ? ': ' + _fmt(d.value, opts) : ''}`))
        .on('mouseout', () => _hideTip(tip))
        .on('click', (ev, d) => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.label, value: d.value || 0, group: d.group, index: graphData.nodes.indexOf(d) }));

    sim.on('tick', () => {
        link.attr('x1', d => d.source.x).attr('y1', d => d.source.y)
            .attr('x2', d => d.target.x).attr('y2', d => d.target.y);
        node.attr('transform', d => `translate(${d.x},${d.y})`);
    });

    return sim;
}

// ── Treemap ───────────────────────────────────────────────────────────────────

function _drawTreemap(d3, svg, data, opts, width, height, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const groups = [...new Set(data.map(d => d.group || 'default'))];

    svg.selectAll('*').remove();

    const root = d3.hierarchy({ children: data }).sum(d => d.value).sort((a, b) => b.value - a.value);
    d3.treemap().size([width, height]).padding(2).round(true)(root);

    const tip = _makeTooltip(d3.select(svg.node().parentElement));

    const cell = svg.selectAll('g').data(root.leaves()).enter().append('g')
        .attr('transform', d => `translate(${d.x0},${d.y0})`);

    cell.append('rect')
        .attr('width', d => d.x1 - d.x0).attr('height', d => d.y1 - d.y0)
        .attr('fill', d => colors[groups.indexOf(d.data.group || 'default') % colors.length])
        .attr('opacity', opts.animate ? 0 : 0.85).attr('rx', 3)
        .style('cursor', 'pointer')
        .on('mouseover', (ev, d) => _showTip(tip, ev, `<strong>${d.data.label}</strong>: ${_fmt(d.data.value, opts)}`))
        .on('mouseout', () => _hideTip(tip))
        .on('click', (ev, d) => dotnet && dotnet.invokeMethodAsync('OnDataPointClickedAsync', { label: d.data.label, value: d.data.value, group: d.data.group, index: data.indexOf(d.data) }))
        .transition().duration(opts.animate ? opts.animationDuration : 0).attr('opacity', 0.85);

    cell.append('text').attr('x', 5).attr('y', 16)
        .style('font-size', '11px').style('fill', '#fff').style('pointer-events', 'none')
        .text(d => { const w = d.x1 - d.x0; return w > 40 ? (d.data.label.length > w / 7 ? d.data.label.slice(0, Math.floor(w / 7)) + '…' : d.data.label) : ''; });
}

// ── Radar ─────────────────────────────────────────────────────────────────────

function _drawRadar(d3, svg, data, opts, width, height, dotnet) {
    const t = _theme();
    const colors = opts.colors || t.palette;
    const cx = width / 2, cy = height / 2;
    const radius = Math.min(width, height) / 2 - 40;
    const levels = 5;

    svg.selectAll('*').remove();
    const g = svg.append('g').attr('transform', `translate(${cx},${cy})`);

    const groups = [...new Set(data.map(d => d.group || 'default'))];
    const axes = [...new Set(data.map(d => d.label))];
    const N = axes.length;
    const angleSlice = (Math.PI * 2) / N;
    const maxVal = d3.max(data, d => d.value) || 1;
    const rScale = d3.scaleLinear().domain([0, maxVal]).range([0, radius]);

    // Grid circles
    for (let l = 1; l <= levels; l++) {
        g.append('circle').attr('r', radius * l / levels)
            .attr('fill', 'none').attr('stroke', t.border).attr('stroke-width', 1);
    }
    // Axes
    axes.forEach((ax, i) => {
        const angle = angleSlice * i - Math.PI / 2;
        g.append('line').attr('x1', 0).attr('y1', 0)
            .attr('x2', radius * Math.cos(angle)).attr('y2', radius * Math.sin(angle))
            .attr('stroke', t.border).attr('stroke-width', 1);
        g.append('text')
            .attr('x', (radius + 16) * Math.cos(angle)).attr('y', (radius + 16) * Math.sin(angle))
            .attr('text-anchor', 'middle').attr('dy', '0.35em')
            .style('font-size', '11px').style('fill', t.textMuted).text(ax);
    });

    const tip = _makeTooltip(d3.select(svg.node().parentElement));

    groups.forEach((grp, gi) => {
        const color = colors[gi % colors.length];
        const grpData = axes.map(ax => {
            const found = data.find(d => d.label === ax && (d.group || 'default') === grp);
            return found ? found.value : 0;
        });
        const points = grpData.map((v, i) => {
            const angle = angleSlice * i - Math.PI / 2;
            return [rScale(v) * Math.cos(angle), rScale(v) * Math.sin(angle)];
        });
        const lineGen = d3.line().x(d => d[0]).y(d => d[1]).curve(d3.curveLinearClosed);
        g.append('path').datum(points)
            .attr('d', lineGen).attr('fill', color).attr('fill-opacity', 0.15)
            .attr('stroke', color).attr('stroke-width', 2);
        points.forEach(([px, py], i) => {
            g.append('circle').attr('cx', px).attr('cy', py).attr('r', 4)
                .attr('fill', color).attr('stroke', t.bg).attr('stroke-width', 1.5)
                .style('cursor', 'pointer')
                .on('mouseover', (ev) => _showTip(tip, ev, `<strong>${axes[i]}</strong>${grp !== 'default' ? ' · ' + grp : ''}: ${_fmt(grpData[i], opts)}`))
                .on('mouseout', () => _hideTip(tip));
        });
    });

    if (opts.showLegend && groups.length > 1) _drawLegend(d3, g, groups, colors, 0, -radius - 30);
}

// ── Legend helper ─────────────────────────────────────────────────────────────

function _drawLegend(d3, g, groups, colors, W, yOffset) {
    const t = _theme();
    const itemW = 90;
    const totalW = groups.length * itemW;
    const startX = (W - totalW) / 2;
    const lg = g.append('g').attr('transform', `translate(${startX},${yOffset})`);
    groups.forEach((grp, i) => {
        lg.append('rect').attr('x', i * itemW).attr('y', 0).attr('width', 10).attr('height', 10)
            .attr('rx', 2).attr('fill', colors[i % colors.length]);
        lg.append('text').attr('x', i * itemW + 14).attr('y', 9)
            .style('font-size', '11px').style('fill', t.textMuted)
            .text(grp.length > 10 ? grp.slice(0, 10) + '…' : grp);
    });
}

// ── Zoom support ──────────────────────────────────────────────────────────────

function _applyZoom(d3, svg, width, height) {
    const zoom = d3.zoom()
        .scaleExtent([0.5, 8])
        .on('zoom', (ev) => svg.select('g').attr('transform', ev.transform));
    svg.call(zoom);
    return zoom;
}

// ── Main draw dispatcher ──────────────────────────────────────────────────────

function _draw(d3, containerId, chartType, data, graphData, opts, dotnet) {
    const inst = _instances.get(containerId);
    if (!inst) return;

    const container = inst.container;
    const rect = container.getBoundingClientRect();
    const width  = rect.width  || 400;
    const height = rect.height || 300;

    // Remove old SVG
    d3.select(container).select('svg.sg-d3-svg').remove();

    const svg = d3.select(container).append('svg')
        .attr('class', 'sg-d3-svg')
        .attr('width', width).attr('height', height)
        .style('display', 'block').style('overflow', 'visible');

    if (opts.enableZoom && chartType !== 'ForceGraph') {
        _applyZoom(d3, svg, width, height);
    }

    switch (chartType) {
        case 'Bar':            _drawBar(d3, svg, data, opts, width, height, false, dotnet); break;
        case 'BarHorizontal':  _drawBar(d3, svg, data, opts, width, height, true,  dotnet); break;
        case 'Line':           _drawLine(d3, svg, data, opts, width, height, false, dotnet); break;
        case 'Area':           _drawLine(d3, svg, data, opts, width, height, true,  dotnet); break;
        case 'Pie':            _drawPie(d3, svg, data, opts, width, height, false, dotnet); break;
        case 'Donut':          _drawPie(d3, svg, data, opts, width, height, true,  dotnet); break;
        case 'Scatter':        _drawScatter(d3, svg, data, opts, width, height, dotnet); break;
        case 'ForceGraph':     inst.sim = _drawForce(d3, svg, graphData, opts, width, height, dotnet); break;
        case 'Treemap':        _drawTreemap(d3, svg, data, opts, width, height, dotnet); break;
        case 'Radar':          _drawRadar(d3, svg, data, opts, width, height, dotnet); break;
    }

    inst.lastChartType = chartType;
    inst.lastData      = data;
    inst.lastGraphData = graphData;
    inst.lastOpts      = opts;
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initD3(dotnetRef, containerRef, containerId, chartType, data, graphData, opts, sources) {
    if (!containerRef) { console.error(`[SgD3] containerRef null for ${containerId}`); return; }

    const d3 = await _ensureD3(sources);

    // Stop any previous simulation
    const prev = _instances.get(containerId);
    if (prev?.sim) { try { prev.sim.stop(); } catch {} }
    if (prev?.resizeObserver) { try { prev.resizeObserver.disconnect(); } catch {} }

    _instances.set(containerId, { container: containerRef, d3, dotnetRef, sim: null, resizeObserver: null });

    _draw(d3, containerId, chartType, data, graphData, opts, dotnetRef);

    const ro = _attachResize(containerId, containerRef, () => {
        const inst = _instances.get(containerId);
        if (inst) _draw(d3, containerId, inst.lastChartType, inst.lastData, inst.lastGraphData, inst.lastOpts, inst.dotnetRef);
    });
    _instances.get(containerId).resizeObserver = ro;
}

export function updateD3(containerId, chartType, data, graphData, opts) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    if (inst.sim) { try { inst.sim.stop(); } catch {} }
    _draw(inst.d3, containerId, chartType, data, graphData, opts, inst.dotnetRef);
}

export function resetZoom(containerId) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    const svg = inst.d3.select(inst.container).select('svg.sg-d3-svg');
    if (!svg.empty()) svg.transition().duration(300).call(inst.d3.zoom().transform, inst.d3.zoomIdentity);
}

export function exportSvg(containerId) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    const svgEl = inst.container.querySelector('svg.sg-d3-svg');
    if (!svgEl) return;
    const serializer = new XMLSerializer();
    const svgStr = serializer.serializeToString(svgEl);
    const blob = new Blob([svgStr], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = `chart-${Date.now()}.svg`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

export function exportPng(containerId) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    const svgEl = inst.container.querySelector('svg.sg-d3-svg');
    if (!svgEl) return;
    const w = svgEl.getAttribute('width') || 400;
    const h = svgEl.getAttribute('height') || 300;
    const serializer = new XMLSerializer();
    const svgStr = serializer.serializeToString(svgEl);
    const img = new Image();
    img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = w; canvas.height = h;
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, w, h);
        ctx.drawImage(img, 0, 0);
        const a = document.createElement('a'); a.href = canvas.toDataURL('image/png');
        a.download = `chart-${Date.now()}.png`;
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    };
    img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgStr)));
}

export function dispose(containerId) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    if (inst.sim) { try { inst.sim.stop(); } catch {} }
    if (inst.resizeObserver) { try { inst.resizeObserver.disconnect(); } catch {} }
    try { inst.d3.select(inst.container).select('svg.sg-d3-svg').remove(); } catch {}
    _instances.delete(containerId);
}
