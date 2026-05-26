// Resizable: pointer-driven width/height resize via 8 handles (or subset).
// Supports aspect ratio lock, grid snap, size tooltip, double-click reset.
// Reports final size to .NET via SetSize(width, height).
// Optionally reports live size during drag via SetSizeLive(width, height).

export function attach(root, dotnet, opts) {
    if (!root) return;
    if (root._sgResizable) detach(root);

    let isDisposed = false;
    const cfg = {
        minWidth:     opts?.minWidth     ?? 80,
        minHeight:    opts?.minHeight    ?? 60,
        maxWidth:     opts?.maxWidth     ?? 4000,
        maxHeight:    opts?.maxHeight    ?? 4000,
        disabled:     opts?.disabled     ?? false,
        liveFeedback: opts?.liveFeedback ?? false,
        aspectRatio:  opts?.aspectRatio  ?? null,
        snapStep:     opts?.snapStep     ?? null,
        showTooltip:  opts?.showTooltip  ?? false,
        handleSize:   opts?.handleSize   ?? 'medium',
    };

    let active = null;
    let currentWidth = null;
    let currentHeight = null;
    let tooltipEl = null;

    // ── Size tooltip ─────────────────────────────────────────────────────────
    function ensureTooltip() {
        if (!cfg.showTooltip) return;
        if (!tooltipEl) {
            tooltipEl = document.createElement('div');
            tooltipEl.className = 'sgc-resizable-tooltip';
            root.appendChild(tooltipEl);
        }
    }

    function updateTooltip(w, h) {
        if (!cfg.showTooltip || !tooltipEl) return;
        tooltipEl.textContent = `${Math.round(w)} \u00d7 ${Math.round(h)} px`;
    }

    function removeTooltip() {
        if (tooltipEl && tooltipEl.parentNode) {
            tooltipEl.parentNode.removeChild(tooltipEl);
        }
        tooltipEl = null;
    }

    // ── Snap helper ──────────────────────────────────────────────────────────
    function snap(v) {
        return cfg.snapStep ? Math.round(v / cfg.snapStep) * cfg.snapStep : v;
    }

    // ── Resize calculation with aspect ratio and snap ─────────────────────────
    function calcSize(dx, dy) {
        let w = active.startW;
        let h = active.startH;
        if (active.dir.includes('e')) w = active.startW + dx;
        if (active.dir.includes('w')) w = active.startW - dx;
        if (active.dir.includes('s')) h = active.startH + dy;
        if (active.dir.includes('n')) h = active.startH - dy;

        // Apply aspect ratio
        if (cfg.aspectRatio) {
            const ratio = cfg.aspectRatio;
            const dirIsHorizontal = active.dir.includes('e') || active.dir.includes('w');
            const dirIsVertical = active.dir.includes('s') || active.dir.includes('n');
            if (dirIsHorizontal && !dirIsVertical) {
                h = w / ratio;
            } else if (dirIsVertical && !dirIsHorizontal) {
                w = h * ratio;
            } else {
                // Corner drag - compute from dominant axis
                const fromW = Math.abs(w / ratio - h);
                const fromH = Math.abs(h * ratio - w);
                if (fromW <= fromH) {
                    h = w / ratio;
                } else {
                    w = h * ratio;
                }
            }
        }

        // Apply snap
        w = snap(w);
        h = snap(h);

        // Clamp
        w = Math.max(cfg.minWidth,  Math.min(cfg.maxWidth,  w));
        h = Math.max(cfg.minHeight, Math.min(cfg.maxHeight, h));

        // Re-apply aspect ratio after clamp to maintain ratio
        if (cfg.aspectRatio) {
            const ratio = cfg.aspectRatio;
            const clampedW = w;
            const clampedH = h;
            // Choose which dimension to trust
            if (clampedW / clampedH > ratio) {
                w = clampedH * ratio;
            } else {
                h = clampedW / ratio;
            }
            // Re-clamp after ratio correction
            w = Math.max(cfg.minWidth,  Math.min(cfg.maxWidth,  w));
            h = Math.max(cfg.minHeight, Math.min(cfg.maxHeight, h));
        }

        return { w, h };
    }

    // ── Pointer events ───────────────────────────────────────────────────────
    function onPointerDown(e) {
        if (isDisposed || !dotnet || cfg.disabled) return;
        const handle = e.target.closest('.sgc-resizable-handle');
        if (!handle || !root.contains(handle)) return;
        if (e.button !== 0) return;
        e.preventDefault();

        const dir  = handle.getAttribute('data-dir') || 'se';
        const rect = root.getBoundingClientRect();
        active = {
            dir,
            startX: e.clientX,
            startY: e.clientY,
            startW: rect.width,
            startH: rect.height,
        };

        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup',   onPointerUp,   { once: true });
        document.addEventListener('pointercancel', onPointerUp, { once: true });

        root.classList.add('sgc-resizable-active');
        ensureTooltip();
    }

    function onPointerMove(e) {
        if (isDisposed || !dotnet || !active) return;
        const dx = e.clientX - active.startX;
        const dy = e.clientY - active.startY;
        const { w, h } = calcSize(dx, dy);
        root.style.width  = w + 'px';
        root.style.height = h + 'px';
        active._w = w;
        active._h = h;
        updateTooltip(w, h);

        // Live feedback — throttled to animation frames
        if (cfg.liveFeedback && !active._rafPending) {
            active._rafPending = true;
            requestAnimationFrame(() => {
                if (!active) return;
                active._rafPending = false;
                const lw = active._w ?? active.startW;
                const lh = active._h ?? active.startH;
                try { dotnet.invokeMethodAsync('SetSizeLive', lw, lh).catch(() => {}); } catch { /* noop */ }
            });
        }
    }

    function onPointerUp() {
        document.removeEventListener('pointermove',   onPointerMove);
        document.removeEventListener('pointerup',     onPointerUp);
        document.removeEventListener('pointercancel', onPointerUp);
        if (!active) return;
        root.classList.remove('sgc-resizable-active');
        const w = active._w ?? active.startW;
        const h = active._h ?? active.startH;
        currentWidth = w;
        currentHeight = h;
        active = null;
        removeTooltip();
        if (!isDisposed && dotnet) {
            try { dotnet.invokeMethodAsync('SetSize', w, h).catch(() => {}); } catch { /* noop */ }
        }
    }

    // ── Double-click to reset ────────────────────────────────────────────────
    function onDblClick(e) {
        if (isDisposed || !dotnet || cfg.disabled) return;
        const handle = e.target.closest('.sgc-resizable-handle');
        if (!handle || !root.contains(handle)) return;
        e.preventDefault();
        try { dotnet.invokeMethodAsync('ResetSize').catch(() => {}); } catch { /* noop */ }
    }

    function reapplySize() {
        if (currentWidth !== null && currentHeight !== null) {
            root.style.width = currentWidth + 'px';
            root.style.height = currentHeight + 'px';
        }
    }

    root.addEventListener('pointerdown', onPointerDown);
    root.addEventListener('dblclick', onDblClick);

    // Apply handle size class
    if (cfg.handleSize && cfg.handleSize !== 'medium') {
        root.classList.add('sgc-resizable-handle-' + cfg.handleSize);
    }

    root._sgResizable = {
        onPointerDown,
        onDblClick,
        reapplySize,
        dispose() { isDisposed = true; dotnet = null; }
    };
}

export function detach(root) {
    if (!root || !root._sgResizable) return;
    const { onPointerDown, onDblClick, dispose } = root._sgResizable;
    if (dispose) dispose();
    root.removeEventListener('pointerdown', onPointerDown);
    root.removeEventListener('dblclick', onDblClick);
    delete root._sgResizable;
}

export function reapplySize(root) {
    if (!root || !root._sgResizable) return;
    root._sgResizable.reapplySize();
}
