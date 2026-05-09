// Resizable: pointer-driven width/height resize via 8 handles (or subset).
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
    };

    let active = null;
    let currentWidth = null;
    let currentHeight = null;

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

        // Listen on document so events keep firing even when cursor leaves the handle
        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup',   onPointerUp,   { once: true });
        document.addEventListener('pointercancel', onPointerUp, { once: true });

        root.classList.add('sgc-resizable-active');
    }

    function onPointerMove(e) {
        if (isDisposed || !dotnet || !active) return;
        const dx = e.clientX - active.startX;
        const dy = e.clientY - active.startY;
        let w = active.startW;
        let h = active.startH;
        if (active.dir.includes('e')) w = active.startW + dx;
        if (active.dir.includes('w')) w = active.startW - dx;
        if (active.dir.includes('s')) h = active.startH + dy;
        if (active.dir.includes('n')) h = active.startH - dy;
        w = Math.max(cfg.minWidth,  Math.min(cfg.maxWidth,  w));
        h = Math.max(cfg.minHeight, Math.min(cfg.maxHeight, h));
        root.style.width  = w + 'px';
        root.style.height = h + 'px';
        active._w = w;
        active._h = h;

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
        if (!isDisposed && dotnet) {
            try { dotnet.invokeMethodAsync('SetSize', w, h).catch(() => {}); } catch { /* noop */ }
        }
    }

    function reapplySize() {
        if (currentWidth !== null && currentHeight !== null) {
            root.style.width = currentWidth + 'px';
            root.style.height = currentHeight + 'px';
        }
    }

    root.addEventListener('pointerdown', onPointerDown);
    root._sgResizable = {
        onPointerDown,
        reapplySize,
        dispose() { isDisposed = true; dotnet = null; }
    };
}

export function detach(root) {
    if (!root || !root._sgResizable) return;
    const { onPointerDown, dispose } = root._sgResizable;
    if (dispose) dispose();
    root.removeEventListener('pointerdown', onPointerDown);
    delete root._sgResizable;
}

export function reapplySize(root) {
    if (!root || !root._sgResizable) return;
    root._sgResizable.reapplySize();
}
