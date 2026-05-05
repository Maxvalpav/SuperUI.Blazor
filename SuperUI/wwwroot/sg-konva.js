// SgKonva - Konva.js Integration Module for SuperUI Blazor
// Provides JS interop for SgKonva component.

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

async function _ensureKonva(sources) {
    if (sources?.konvaScript) await _loadScript(sources.konvaScript);
    let K = window.Konva;
    let n = 0;
    while (!K && n++ < 80) { await new Promise(r => setTimeout(r, 100)); K = window.Konva; }
    if (!K) throw new Error('Konva.js not loaded');
    return K;
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Room colours (light theme) ────────────────────────────────────────────────

const ROOM_BG = '#f8fafc';          // canvas background
const ROOM_WALL = '#334155';        // wall colour

const ROOM_STATUS_FILL = {
    Available:   '#dbeafe',   // blue-100
    Occupied:    '#dcfce7',   // green-100
    Reserved:    '#fef9c3',   // yellow-100
    Maintenance: '#ede9fe',   // violet-100
    Closed:      '#fee2e2',   // red-100
};
const ROOM_STATUS_STROKE = {
    Available:   '#3b82f6',   // blue-500
    Occupied:    '#16a34a',   // green-600
    Reserved:    '#ca8a04',   // yellow-600
    Maintenance: '#7c3aed',   // violet-600
    Closed:      '#dc2626',   // red-600
};
const ROOM_STATUS_TEXT_COLOR = {
    Available:   '#1d4ed8',
    Occupied:    '#15803d',
    Reserved:    '#a16207',
    Maintenance: '#6d28d9',
    Closed:      '#b91c1c',
};
const ROOM_NAME_COLOR = '#1e293b';   // slate-800
const ROOM_OCC_COLOR  = '#64748b';   // slate-500
const ROOM_TYPE_ICON = {
    Office:      '🖥',
    MeetingRoom: '📋',
    OpenSpace:   '👥',
    Kitchen:     '☕',
    Restroom:    '🚻',
    Corridor:    '',
    Storage:     '📦',
    ServerRoom:  '🖧',
    Reception:   '🛎',
    Lobby:       '🏛',
    Stairs:      '🪜',
    Elevator:    '🛗',
};

// ── Tooltip ───────────────────────────────────────────────────────────────────

function _makeTooltip(container) {
    let tip = container.querySelector('.sg-konva-tip');
    if (!tip) {
        tip = document.createElement('div');
        tip.className = 'sg-konva-tip';
        tip.style.cssText = `
            position:absolute;pointer-events:none;
            background:#ffffff;color:#1e293b;
            padding:8px 12px;border-radius:6px;font-size:12px;
            line-height:1.5;white-space:nowrap;opacity:0;
            transition:opacity 0.12s;z-index:10;
            box-shadow:0 4px 16px rgba(0,0,0,0.12),0 1px 4px rgba(0,0,0,0.08);
            border:1px solid #e2e8f0;
        `;
        container.style.position = 'relative';
        container.appendChild(tip);
    }
    return tip;
}

function _showTip(tip, x, y, html) {
    tip.innerHTML = html;
    tip.style.left = (x + 14) + 'px';
    tip.style.top  = (y - 10) + 'px';
    tip.style.opacity = '1';
}

function _hideTip(tip) { tip.style.opacity = '0'; }

// ── Floor plan builder ────────────────────────────────────────────────────────

function _buildFloorPlan(Konva, stage, layer, plan, dotnetRef, opts) {
    layer.destroyChildren();

    const scaleX = stage.width()  / plan.width;
    const scaleY = stage.height() / plan.height;
    const scale  = Math.min(scaleX, scaleY);
    const offX   = (stage.width()  - plan.width  * scale) / 2;
    const offY   = (stage.height() - plan.height * scale) / 2;

    const tip = _makeTooltip(stage.container());

    // Background
    layer.add(new Konva.Rect({
        x: offX, y: offY,
        width:  plan.width  * scale,
        height: plan.height * scale,
        fill: ROOM_BG,
        stroke: '#cbd5e1',
        strokeWidth: 1.5,
    }));

    // Rooms
    (plan.rooms ?? []).forEach(room => {
        const rx = offX + room.x * scale;
        const ry = offY + room.y * scale;
        const rw = room.width  * scale;
        const rh = room.height * scale;

        const fill   = ROOM_STATUS_FILL[room.status]   ?? '#dbeafe';
        const stroke = ROOM_STATUS_STROKE[room.status] ?? '#3b82f6';
        const isDraggable = opts?.draggable === true;

        const group = new Konva.Group({
            x: rx, y: ry,
            draggable: isDraggable,
            id: room.id,
        });

        // Room body
        const rect = new Konva.Rect({
            width: rw, height: rh,
            fill, stroke, strokeWidth: 1.5,
            cornerRadius: 3,
            shadowColor: stroke,
            shadowBlur: 4,
            shadowOpacity: 0.2,
        });
        group.add(rect);

        // Occupancy bar (bottom strip)
        if (room.capacity && room.currentOccupancy != null) {
            const pct = Math.min(1, room.currentOccupancy / room.capacity);
            const barH = Math.max(3, rh * 0.06);
            group.add(new Konva.Rect({
                y: rh - barH, width: rw, height: barH,
                fill: '#0f172a', cornerRadius: [0, 0, 4, 4],
            }));
            group.add(new Konva.Rect({
                y: rh - barH, width: rw * pct, height: barH,
                fill: pct > 0.85 ? '#ef4444' : pct > 0.6 ? '#f59e0b' : '#22c55e',
                cornerRadius: [0, 0, pct >= 0.99 ? 4 : 0, 4],
            }));
        }

        // Icon
        const icon = ROOM_TYPE_ICON[room.type] ?? '';
        if (icon && rw > 30 && rh > 30) {
            group.add(new Konva.Text({
                x: 4, y: 5,
                text: icon,
                fontSize: Math.min(16, rw * 0.16, rh * 0.24),
                listening: false,
            }));
        }

        // Room name
        if (rw > 40 && rh > 24) {
            const fontSize = Math.min(12, rw * 0.11, rh * 0.16);
            // Position name in upper-middle area, leaving room for status at bottom
            const nameY = rh > 60 ? rh * 0.28 : rh * 0.22;
            group.add(new Konva.Text({
                x: 4, y: nameY,
                width: rw - 8,
                text: room.name,
                fontSize,
                fontStyle: 'bold',
                fill: ROOM_NAME_COLOR,
                align: 'center',
                ellipsis: true,
                listening: false,
            }));
        }

        // Capacity label (occupancy counter)
        if (room.capacity && rw > 50 && rh > 70) {
            const occ = room.currentOccupancy ?? 0;
            const pct = room.capacity > 0 ? occ / room.capacity : 0;
            const occColor = pct > 0.85 ? '#dc2626' : pct > 0.6 ? '#ca8a04' : ROOM_OCC_COLOR;
            group.add(new Konva.Text({
                x: 4, y: rh * 0.54,
                width: rw - 8,
                text: `👥 ${occ}/${room.capacity}`,
                fontSize: Math.min(10, rw * 0.09),
                fill: occColor,
                align: 'center',
                listening: false,
            }));
        }

        // ── Status badge ──────────────────────────────────────────────────────
        const STATUS_LABEL = {
            Available:   '● Свободно',
            Occupied:    '● Занято',
            Reserved:    '● Резерв',
            Maintenance: '● Обслуж.',
            Closed:      '● Закрыто',
        };
        const statusText  = STATUS_LABEL[room.status] ?? room.status;
        const statusColor = ROOM_STATUS_TEXT_COLOR[room.status] ?? '#64748b';

        // Only show if room is large enough
        if (rw > 45 && rh > 36) {
            const sFontSize = Math.min(10, rw * 0.09, rh * 0.14);
            const sPad = 3;

            const sText = new Konva.Text({
                text: statusText,
                fontSize: sFontSize,
                fontStyle: 'bold',
                fill: statusColor,
                listening: false,
            });
            const sW = sText.width() + sPad * 2;
            const sH = sFontSize + sPad * 2;
            const sX = (rw - sW) / 2;
            const sY = rh - sH - (room.capacity ? rh * 0.07 : 4);

            // Light badge background
            group.add(new Konva.Rect({
                x: sX - 1, y: sY - 1,
                width: sW + 2, height: sH + 2,
                fill: 'rgba(255,255,255,0.75)',
                stroke: statusColor,
                strokeWidth: 0.5,
                cornerRadius: 3,
                listening: false,
            }));
            group.add(new Konva.Text({
                x: sX + sPad, y: sY + sPad,
                text: statusText,
                fontSize: sFontSize,
                fontStyle: 'bold',
                fill: statusColor,
                listening: false,
            }));
        }

        // Events
        group.on('mouseenter', (e) => {
            rect.strokeWidth(2.5);
            rect.shadowBlur(10);
            rect.shadowOpacity(0.35);
            stage.container().style.cursor = 'pointer';
            const pos = stage.getPointerPosition();
            const occ = room.currentOccupancy != null ? `<br/>👥 ${room.currentOccupancy}/${room.capacity ?? '?'}` : '';
            _showTip(tip, pos.x, pos.y,
                `<b>${room.name}</b><br/>${room.type} · ${room.status}${occ}`);
            layer.batchDraw();
        });
        group.on('mouseleave', () => {
            rect.strokeWidth(1.5);
            rect.shadowBlur(4);
            rect.shadowOpacity(0.2);
            stage.container().style.cursor = 'default';
            _hideTip(tip);
            layer.batchDraw();
        });
        group.on('click tap', (e) => {
            const pos = stage.getPointerPosition();
            try {
                dotnetRef.invokeMethodAsync('OnShapeClickedAsync', {
                    id:       String(room.id),
                    typeName: String(room.type ?? ''),
                    x:        pos ? pos.x : 0,
                    y:        pos ? pos.y : 0,
                    data:     JSON.stringify(room),
                });
            } catch {}
        });
        group.on('dragend', () => {
            const newX = (group.x() - offX) / scale;
            const newY = (group.y() - offY) / scale;
            try {
                dotnetRef.invokeMethodAsync('OnShapeDraggedAsync', {
                    id: String(room.id),
                    x:  newX,
                    y:  newY,
                });
            } catch {}
        });

        layer.add(group);
    });

    // Walls
    (plan.walls ?? []).forEach(wall => {
        layer.add(new Konva.Line({
            points: [
                offX + wall.x1 * scale, offY + wall.y1 * scale,
                offX + wall.x2 * scale, offY + wall.y2 * scale,
            ],
            stroke: ROOM_WALL,
            strokeWidth: Math.max(1.5, (wall.thickness ?? 8) * scale * 0.5),
            lineCap: 'round',
            listening: false,
        }));
    });

    layer.batchDraw();
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initKonva(dotnetRef, containerRef, instanceId, plan, opts, sources) {
    await disposeKonva(instanceId);

    const Konva = await _ensureKonva(sources);

    const w = containerRef.clientWidth  || 800;
    const h = containerRef.clientHeight || 500;

    const stage = new Konva.Stage({ container: containerRef, width: w, height: h });
    const layer = new Konva.Layer();
    stage.add(layer);

    // Zoom with wheel
    stage.on('wheel', (e) => {
        e.evt.preventDefault();
        const oldScale = stage.scaleX();
        const pointer  = stage.getPointerPosition();
        const mousePointTo = {
            x: (pointer.x - stage.x()) / oldScale,
            y: (pointer.y - stage.y()) / oldScale,
        };
        const direction = e.evt.deltaY > 0 ? -1 : 1;
        const newScale   = Math.max(0.3, Math.min(5, oldScale + direction * 0.08));
        stage.scale({ x: newScale, y: newScale });
        stage.position({
            x: pointer.x - mousePointTo.x * newScale,
            y: pointer.y - mousePointTo.y * newScale,
        });
        stage.batchDraw();
    });

    if (plan) _buildFloorPlan(Konva, stage, layer, plan, dotnetRef, opts);

    // Resize observer
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => {
                const nw = containerRef.clientWidth  || 800;
                const nh = containerRef.clientHeight || 500;
                stage.width(nw); stage.height(nh);
                if (plan) _buildFloorPlan(Konva, stage, layer, plan, dotnetRef, opts);
            });
        });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { stage, layer, Konva, dotnetRef, ro, plan, opts });
}

export async function updateFloorPlan(instanceId, plan, opts) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.plan = plan;
    inst.opts = opts;
    _buildFloorPlan(inst.Konva, inst.stage, inst.layer, plan, inst.dotnetRef, opts);
}

export function updateRoomStatus(instanceId, roomId, status) {
    const inst = _instances.get(instanceId);
    if (!inst || !inst.plan) return;
    const room = inst.plan.rooms?.find(r => String(r.id) === String(roomId));
    if (room) {
        room.status = status;
        _buildFloorPlan(inst.Konva, inst.stage, inst.layer, inst.plan, inst.dotnetRef, inst.opts);
    }
}

export function fitView(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.stage.scale({ x: 1, y: 1 });
    inst.stage.position({ x: 0, y: 0 });
    inst.stage.batchDraw();
}

export function zoomIn(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const s = Math.min(5, inst.stage.scaleX() * 1.2);
    inst.stage.scale({ x: s, y: s });
    inst.stage.batchDraw();
}

export function zoomOut(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const s = Math.max(0.3, inst.stage.scaleX() / 1.2);
    inst.stage.scale({ x: s, y: s });
    inst.stage.batchDraw();
}

export function exportPng(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const url = inst.stage.toDataURL({ pixelRatio: 2 });
    const a = document.createElement('a');
    a.href = url; a.download = `floorplan-${Date.now()}.png`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

export async function disposeKonva(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.stage.destroy(); } catch {}
    _instances.delete(instanceId);
}
