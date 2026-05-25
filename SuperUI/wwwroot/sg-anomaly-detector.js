'use strict';
const _instances = new Map();

export function init(dotNetRef, canvasEl, instanceId) {
    _instances.set(instanceId, { dotNetRef, canvas: canvasEl, ctx: canvasEl.getContext('2d') });
}

export function render(instanceId, points, anomalies, opts) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { canvas, ctx } = inst;
    const W = canvas.width, H = canvas.height;
    const pad = { top: 20, right: 20, bottom: 40, left: 60 };
    const plotW = W - pad.left - pad.right, plotH = H - pad.top - pad.bottom;
    ctx.clearRect(0, 0, W, H);
    if (!points || points.length === 0) return;

    const vals = points.map(p => p.value);
    const minV = Math.min(...vals), maxV = Math.max(...vals);
    const rangeV = maxV - minV || 1;
    const xScale = i => pad.left + (i / (points.length - 1)) * plotW;
    const yScale = v => pad.top + plotH - ((v - minV) / rangeV) * plotH;

    // Anomaly zones
    ctx.save();
    for (const a of (anomalies || [])) {
        const x1 = xScale(a.startIndex), x2 = xScale(a.endIndex);
        ctx.fillStyle = 'rgba(239,68,68,0.18)';
        ctx.fillRect(x1, pad.top, x2 - x1 + 2, plotH);
        ctx.strokeStyle = 'rgba(239,68,68,0.5)';
        ctx.lineWidth = 1;
        ctx.strokeRect(x1, pad.top, x2 - x1 + 2, plotH);
    }
    ctx.restore();

    // Line
    ctx.beginPath();
    ctx.strokeStyle = '#3b82f6';
    ctx.lineWidth = 1.5;
    points.forEach((p, i) => {
        const x = xScale(i), y = yScale(p.value);
        i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
    });
    ctx.stroke();

    // Axes
    ctx.strokeStyle = '#94a3b8';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(pad.left, pad.top);
    ctx.lineTo(pad.left, pad.top + plotH);
    ctx.lineTo(pad.left + plotW, pad.top + plotH);
    ctx.stroke();

    // Y ticks
    ctx.fillStyle = '#64748b';
    ctx.font = '10px system-ui';
    ctx.textAlign = 'right';
    for (let i = 0; i <= 4; i++) {
        const v = minV + (rangeV * i / 4);
        const y = yScale(v);
        ctx.fillText(v.toFixed(1), pad.left - 4, y + 3);
    }

    // Y axis label
    if (opts && opts.yAxisLabel) {
        ctx.save();
        ctx.translate(12, pad.top + plotH / 2);
        ctx.rotate(-Math.PI / 2);
        ctx.textAlign = 'center';
        ctx.fillStyle = '#94a3b8';
        ctx.font = '10px system-ui';
        ctx.fillText(opts.yAxisLabel, 0, 0);
        ctx.restore();
    }

    // X ticks (first, mid, last)
    ctx.textAlign = 'center';
    [0, Math.floor(points.length / 2), points.length - 1].forEach(i => {
        if (i < points.length) {
            const d = new Date(points[i].timestamp);
            ctx.fillText(d.toLocaleTimeString(), xScale(i), pad.top + plotH + 14);
        }
    });
}

export function dispose(instanceId) {
    _instances.delete(instanceId);
}
