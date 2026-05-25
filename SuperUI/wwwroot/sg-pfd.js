'use strict';
const _pfds = new Map();

export function init(dotNetRef, canvasEl, instanceId) {
    const ctx = canvasEl.getContext('2d');
    _pfds.set(instanceId, {
        ctx, canvas: canvasEl, raf: null,
        telemetry: { pitch: 0, roll: 0, heading: 0, altitude: 1000, speed: 250, verticalSpeed: 0, throttle: 50 }
    });
    _startLoop(instanceId);
}

function _startLoop(id) {
    const inst = _pfds.get(id);
    if (!inst) return;
    function loop() {
        if (!_pfds.has(id)) return;
        _draw(id);
        inst.raf = requestAnimationFrame(loop);
    }
    inst.raf = requestAnimationFrame(loop);
}

export function update(instanceId, t) {
    const inst = _pfds.get(instanceId);
    if (inst) inst.telemetry = t;
}

function _draw(id) {
    const inst = _pfds.get(id);
    if (!inst) return;
    const { ctx, canvas, telemetry: t } = inst;
    const W = canvas.width, H = canvas.height, cx = W / 2, cy = H / 2;
    ctx.clearRect(0, 0, W, H);

    // Background
    ctx.fillStyle = '#1a1a2e';
    ctx.fillRect(0, 0, W, H);

    // Horizon clip circle
    const r = Math.min(W, H) * 0.38;
    ctx.save();
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.clip();

    // Sky & ground
    ctx.save();
    ctx.translate(cx, cy);
    ctx.rotate(t.roll * Math.PI / 180);
    const pitchOffset = t.pitch * (r / 45);
    ctx.fillStyle = '#1e6bb8';
    ctx.fillRect(-W, -H + pitchOffset, W * 2, H);
    ctx.fillStyle = '#8B6914';
    ctx.fillRect(-W, pitchOffset, W * 2, H);

    // Horizon line
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(-W, pitchOffset);
    ctx.lineTo(W, pitchOffset);
    ctx.stroke();

    // Pitch ladder
    ctx.strokeStyle = 'rgba(255,255,255,0.7)';
    ctx.fillStyle = '#fff';
    ctx.font = '10px monospace';
    ctx.textAlign = 'right';
    ctx.lineWidth = 1;
    for (let deg = -30; deg <= 30; deg += 10) {
        if (deg === 0) continue;
        const y = pitchOffset - deg * (r / 45);
        const len = deg % 20 === 0 ? 40 : 25;
        ctx.beginPath();
        ctx.moveTo(-len, y);
        ctx.lineTo(len, y);
        ctx.stroke();
        ctx.fillText(Math.abs(deg), -len - 3, y + 3);
    }
    ctx.restore();
    ctx.restore();

    // Horizon circle border
    ctx.strokeStyle = '#555';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.stroke();

    // Roll arc
    ctx.save();
    ctx.translate(cx, cy);
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(0, 0, r + 8, -Math.PI * 0.75, -Math.PI * 0.25);
    ctx.stroke();

    // Roll pointer
    ctx.rotate(t.roll * Math.PI / 180);
    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.moveTo(0, -(r + 8));
    ctx.lineTo(-5, -(r + 16));
    ctx.lineTo(5, -(r + 16));
    ctx.closePath();
    ctx.fill();
    ctx.restore();

    // Center cross
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(cx - 30, cy);
    ctx.lineTo(cx - 10, cy);
    ctx.moveTo(cx + 10, cy);
    ctx.lineTo(cx + 30, cy);
    ctx.moveTo(cx, cy - 10);
    ctx.lineTo(cx, cy + 10);
    ctx.stroke();

    // Speed tape (left)
    _drawTape(ctx, 10, cy, 50, H * 0.7, t.speed, 'SPD', 'km/h', 10, 5);
    // Altitude tape (right)
    _drawTape(ctx, W - 60, cy, 50, H * 0.7, t.altitude, 'ALT', 'm', 50, 10);
    // Heading tape (bottom)
    _drawHeading(ctx, cx, H - 30, W * 0.6, 40, t.heading);

    // Throttle
    ctx.fillStyle = 'rgba(0,0,0,0.5)';
    ctx.fillRect(W - 30, 20, 16, H * 0.4);
    ctx.fillStyle = '#22c55e';
    ctx.fillRect(W - 30, 20 + H * 0.4 * (1 - t.throttle / 100), 16, H * 0.4 * (t.throttle / 100));
    ctx.strokeStyle = '#555';
    ctx.lineWidth = 1;
    ctx.strokeRect(W - 30, 20, 16, H * 0.4);
    ctx.fillStyle = '#fff';
    ctx.font = '9px monospace';
    ctx.textAlign = 'center';
    ctx.fillText('THR', W - 22, 16);
    ctx.fillText(Math.round(t.throttle) + '%', W - 22, 20 + H * 0.4 + 12);
}

function _drawTape(ctx, x, cy, w, h, value, label, unit, step, bigStep) {
    ctx.fillStyle = 'rgba(0,0,0,0.6)';
    ctx.fillRect(x, cy - h / 2, w, h);
    ctx.strokeStyle = '#555';
    ctx.lineWidth = 1;
    ctx.strokeRect(x, cy - h / 2, w, h);
    ctx.fillStyle = '#fff';
    ctx.font = '9px monospace';
    ctx.textAlign = 'center';
    ctx.fillText(label, x + w / 2, cy - h / 2 - 4);

    const pixPerUnit = h / (step * 8);
    ctx.save();
    ctx.beginPath();
    ctx.rect(x, cy - h / 2, w, h);
    ctx.clip();
    ctx.strokeStyle = '#aaa';
    ctx.fillStyle = '#ccc';
    ctx.font = '9px monospace';
    ctx.textAlign = 'right';
    ctx.lineWidth = 1;
    for (let v = Math.floor(value / step - 5) * step; v <= value + 5 * step; v += step) {
        const y = cy + (value - v) * pixPerUnit;
        const len = v % bigStep === 0 ? 12 : 6;
        ctx.beginPath();
        ctx.moveTo(x + w - len, y);
        ctx.lineTo(x + w, y);
        ctx.stroke();
        if (v % bigStep === 0) ctx.fillText(v, x + w - len - 2, y + 3);
    }
    ctx.restore();

    // Current value box
    ctx.fillStyle = '#000';
    ctx.fillRect(x, cy - 10, w, 20);
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(x, cy - 10, w, 20);
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 11px monospace';
    ctx.textAlign = 'center';
    ctx.fillText(Math.round(value), x + w / 2, cy + 4);
}

function _drawHeading(ctx, cx, y, w, h, heading) {
    ctx.fillStyle = 'rgba(0,0,0,0.6)';
    ctx.fillRect(cx - w / 2, y - h / 2, w, h);
    ctx.strokeStyle = '#555';
    ctx.lineWidth = 1;
    ctx.strokeRect(cx - w / 2, y - h / 2, w, h);
    ctx.save();
    ctx.beginPath();
    ctx.rect(cx - w / 2, y - h / 2, w, h);
    ctx.clip();
    const degPerPx = w / 60;
    ctx.strokeStyle = '#aaa';
    ctx.fillStyle = '#ccc';
    ctx.font = '9px monospace';
    ctx.textAlign = 'center';
    ctx.lineWidth = 1;
    for (let d = heading - 35; d <= heading + 35; d += 5) {
        const nd = ((d % 360) + 360) % 360;
        const px = cx + (d - heading) * degPerPx;
        const len = d % 10 === 0 ? 8 : 4;
        ctx.beginPath();
        ctx.moveTo(px, y - h / 2);
        ctx.lineTo(px, y - h / 2 + len);
        ctx.stroke();
        if (d % 10 === 0) {
            const lbl = nd === 0 ? 'N' : nd === 90 ? 'E' : nd === 180 ? 'S' : nd === 270 ? 'W' : nd;
            ctx.fillText(lbl, px, y - h / 2 + len + 10);
        }
    }
    ctx.restore();
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 11px monospace';
    ctx.textAlign = 'center';
    ctx.fillText(Math.round(heading) + '\u00b0', cx, y + 5);

    // Center pointer
    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.moveTo(cx, y - h / 2 - 4);
    ctx.lineTo(cx - 4, y - h / 2 + 2);
    ctx.lineTo(cx + 4, y - h / 2 + 2);
    ctx.closePath();
    ctx.fill();
}

export function dispose(instanceId) {
    const inst = _pfds.get(instanceId);
    if (inst && inst.raf) cancelAnimationFrame(inst.raf);
    _pfds.delete(instanceId);
}
