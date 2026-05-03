// Pointer-based drag-reorder and corner resize for SgDashboard.
// Avoids HTML5 DnD because Blazor Server's SignalR latency makes it unreliable.

const MIN_DRAG_DISTANCE = 4;

export function attach(root, dotnet) {
    if (!root) return;
    if (root._sgDashAttached) detach(root);

    let isDisposed = false;
    const state = {
        mode: null,           // 'drag' | 'resize' | null
        sourceId: null,
        targetId: null,
        ghost: null,
        startX: 0,
        startY: 0,
        widgetEl: null,
        startColSpan: 1,
        startRowSpan: 1,
        colWidth: 0,
        rowHeight: 0,
        gap: 0,
        pointerId: 0,
        armed: false,
        isDisposed: false,
        dispose: function() {
            isDisposed = true;
            dotnet = null;
        }
    };

    function getColumns() {
        const n = parseInt(root.getAttribute('data-columns') || '3', 10);
        return Number.isFinite(n) && n > 0 ? n : 3;
    }

    function getGridMetrics(widgetEl) {
        const rootRect = root.getBoundingClientRect();
        const cs = window.getComputedStyle(root);
        const gap = parseFloat(cs.columnGap || cs.gap || '0') || 0;
        const rowGap = parseFloat(cs.rowGap || cs.gap || '0') || 0;
        const columns = getColumns();
        const colWidth = (rootRect.width - gap * (columns - 1)) / columns;
        const wRect = widgetEl.getBoundingClientRect();
        const startColSpan = Math.max(1, Math.round((wRect.width + gap) / (colWidth + gap)));
        // For row height we infer from the widget's current bbox / current rowSpan attr.
        const currentRowSpan = parseInt(widgetEl.getAttribute('data-row-span') || '1', 10) || 1;
        const rowHeight = (wRect.height - rowGap * (currentRowSpan - 1)) / currentRowSpan;
        return { colWidth, rowHeight, gap, rowGap, columns, startColSpan, startRowSpan: currentRowSpan };
    }

    function clearTargetHighlight() {
        if (state.targetId) {
            const t = root.querySelector(`[data-widget-id="${cssEscape(state.targetId)}"]`);
            t?.classList.remove('sgc-drop-target');
            state.targetId = null;
        }
    }

    function cssEscape(v) {
        return (window.CSS && CSS.escape) ? CSS.escape(v) : String(v).replace(/"/g, '\\"');
    }

    function findWidgetUnder(x, y) {
        const el = document.elementFromPoint(x, y);
        if (!el) return null;
        const w = el.closest('.sgc-dashboard-widget');
        if (!w || !root.contains(w)) return null;
        return w;
    }

    function buildGhost(headerEl, widgetEl) {
        const wRect = widgetEl.getBoundingClientRect();
        const ghost = document.createElement('div');
        ghost.className = 'sgc-dashboard-widget-drag-ghost';
        ghost.style.width = wRect.width + 'px';
        ghost.style.left = wRect.left + 'px';
        ghost.style.top = wRect.top + 'px';
        const headerClone = headerEl.cloneNode(true);
        headerClone.querySelectorAll('button').forEach(b => b.remove());
        ghost.appendChild(headerClone);
        document.body.appendChild(ghost);
        return ghost;
    }

    function onPointerDown(e) {
        if (isDisposed || !dotnet || e.button !== 0 || state.mode) return;

        const handle = e.target.closest('.sgc-dashboard-widget-resize-handle');
        if (handle && root.contains(handle)) {
            const widget = handle.closest('.sgc-dashboard-widget');
            if (!widget) return;
            e.preventDefault();
            const m = getGridMetrics(widget);
            state.mode = 'resize';
            state.widgetEl = widget;
            state.sourceId = widget.getAttribute('data-widget-id');
            state.startX = e.clientX;
            state.startY = e.clientY;
            state.startColSpan = m.startColSpan;
            state.startRowSpan = m.startRowSpan;
            state.colWidth = m.colWidth;
            state.rowHeight = m.rowHeight;
            state.gap = m.gap;
            state.pointerId = e.pointerId;
            try { handle.setPointerCapture(e.pointerId); } catch { /* noop */ }
            window.addEventListener('pointermove', onPointerMove);
            window.addEventListener('pointerup', onPointerUp, { once: true });
            window.addEventListener('pointercancel', onPointerUp, { once: true });
            return;
        }

        const header = e.target.closest('.sgc-dashboard-widget-header');
        if (!header || !root.contains(header)) return;
        // Don't start drag if the press began on a button/control inside the header.
        if (e.target.closest('button')) return;

        const widget = header.closest('.sgc-dashboard-widget');
        if (!widget) return;

        e.preventDefault();
        state.mode = 'drag';
        state.armed = false;
        state.widgetEl = widget;
        state.sourceId = widget.getAttribute('data-widget-id');
        state.startX = e.clientX;
        state.startY = e.clientY;
        state.pointerId = e.pointerId;
        state._headerEl = header;
        try { header.setPointerCapture(e.pointerId); } catch { /* noop */ }
        window.addEventListener('pointermove', onPointerMove);
        window.addEventListener('pointerup', onPointerUp, { once: true });
        window.addEventListener('pointercancel', onPointerUp, { once: true });
    }

    function onPointerMove(e) {
        if (isDisposed || !dotnet || !state.mode) return;

        if (state.mode === 'drag') {
            const dx = e.clientX - state.startX;
            const dy = e.clientY - state.startY;
            if (!state.armed) {
                if (Math.hypot(dx, dy) < MIN_DRAG_DISTANCE) return;
                state.armed = true;
                state.widgetEl.classList.add('sgc-dragging');
                state.ghost = buildGhost(state._headerEl, state.widgetEl);
            }
            if (state.ghost) {
                state.ghost.style.transform = `translate(${dx}px, ${dy}px)`;
            }
            // Hide ghost briefly to find the element underneath.
            const prevPe = state.ghost ? state.ghost.style.pointerEvents : '';
            if (state.ghost) state.ghost.style.display = 'none';
            const overWidget = findWidgetUnder(e.clientX, e.clientY);
            if (state.ghost) state.ghost.style.display = '';
            const overId = overWidget?.getAttribute('data-widget-id') || null;
            if (overId !== state.targetId) {
                clearTargetHighlight();
                if (overId && overId !== state.sourceId && overWidget) {
                    overWidget.classList.add('sgc-drop-target');
                    state.targetId = overId;
                }
            }
            return;
        }

        if (state.mode === 'resize') {
            const dx = e.clientX - state.startX;
            const dy = e.clientY - state.startY;
            const minCol = parseInt(root.getAttribute('data-min-col') || '1', 10) || 1;
            const maxCol = parseInt(root.getAttribute('data-max-col') || '6', 10) || 6;
            const columns = getColumns();
            const denomX = state.colWidth + state.gap;
            const denomY = state.rowHeight + state.gap;
            let col = state.startColSpan + Math.round(dx / Math.max(1, denomX));
            let row = state.startRowSpan + Math.round(dy / Math.max(1, denomY));
            col = Math.max(minCol, Math.min(maxCol, Math.min(columns, col)));
            row = Math.max(1, Math.min(6, row));
            state.widgetEl.style.gridColumn = `span ${col}`;
            state.widgetEl.style.gridRow = `span ${row}`;
            state._pendingCol = col;
            state._pendingRow = row;
        }
    }

    function onPointerUp() {
        window.removeEventListener('pointermove', onPointerMove);
        if (!state.mode) return;

        if (state.mode === 'drag') {
            const src = state.sourceId;
            const tgt = state.targetId;
            if (state.widgetEl) state.widgetEl.classList.remove('sgc-dragging');
            clearTargetHighlight();
            if (state.ghost && state.ghost.parentNode) state.ghost.parentNode.removeChild(state.ghost);
            state.ghost = null;
            if (state.armed && src && tgt && src !== tgt && !isDisposed && dotnet) {
                try { dotnet.invokeMethodAsync('JsReorder', src, tgt).catch(() => {}); } catch { /* noop */ }
            }
        } else if (state.mode === 'resize') {
            const id = state.sourceId;
            const col = state._pendingCol ?? state.startColSpan;
            const row = state._pendingRow ?? state.startRowSpan;
            // Clear inline styles — Blazor will re-render with the new spans from C#.
            if (state.widgetEl) {
                state.widgetEl.style.gridColumn = '';
                state.widgetEl.style.gridRow = '';
            }
            if (id && (col !== state.startColSpan || row !== state.startRowSpan) && !isDisposed && dotnet) {
                try { dotnet.invokeMethodAsync('JsResize', id, col, row).catch(() => {}); } catch { /* noop */ }
            }
        }

        state.mode = null;
        state.armed = false;
        state.sourceId = null;
        state.widgetEl = null;
        state._headerEl = null;
        state._pendingCol = undefined;
        state._pendingRow = undefined;
    }

    root.addEventListener('pointerdown', onPointerDown);
    root._sgDashAttached = { onPointerDown, onPointerMove, onPointerUp, state };
}

export function detach(root) {
    if (!root || !root._sgDashAttached) return;
    const { onPointerDown, onPointerMove, onPointerUp, state } = root._sgDashAttached;
    
    if (state && state.dispose) {
        state.dispose();
    }
    
    root.removeEventListener('pointerdown', onPointerDown);
    window.removeEventListener('pointermove', onPointerMove);
    window.removeEventListener('pointerup', onPointerUp);
    window.removeEventListener('pointercancel', onPointerUp);
    if (state?.ghost && state.ghost.parentNode) state.ghost.parentNode.removeChild(state.ghost);
    delete root._sgDashAttached;
}
