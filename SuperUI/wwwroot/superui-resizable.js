// Resizable: pointer-driven width/height resize via 8 handles (or subset).
// Reports final size to .NET via SetSize(width, height).

export function attach(root, dotnet, opts) {
    if (!root) return;
    if (root._sgResizable) detach(root);

    const cfg = {
        minWidth: opts?.minWidth ?? 80,
        minHeight: opts?.minHeight ?? 60,
        maxWidth: opts?.maxWidth ?? 4000,
        maxHeight: opts?.maxHeight ?? 4000,
    };

    let active = null;

    function onPointerDown(e) {
        const handle = e.target.closest('.sgc-resizable-handle');
        if (!handle || !root.contains(handle)) return;
        if (e.button !== 0) return;
        e.preventDefault();
        const dir = handle.getAttribute('data-dir') || 'se';
        const rect = root.getBoundingClientRect();
        active = {
            dir,
            startX: e.clientX,
            startY: e.clientY,
            startW: rect.width,
            startH: rect.height,
            handle,
        };
        try { handle.setPointerCapture(e.pointerId); } catch { /* noop */ }
        window.addEventListener('pointermove', onPointerMove);
        window.addEventListener('pointerup', onPointerUp, { once: true });
        window.addEventListener('pointercancel', onPointerUp, { once: true });
        root.classList.add('sgc-resizable-active');
    }

    function onPointerMove(e) {
        if (!active) return;
        const dx = e.clientX - active.startX;
        const dy = e.clientY - active.startY;
        let w = active.startW;
        let h = active.startH;
        if (active.dir.includes('e')) w = active.startW + dx;
        if (active.dir.includes('w')) w = active.startW - dx;
        if (active.dir.includes('s')) h = active.startH + dy;
        if (active.dir.includes('n')) h = active.startH - dy;
        w = Math.max(cfg.minWidth, Math.min(cfg.maxWidth, w));
        h = Math.max(cfg.minHeight, Math.min(cfg.maxHeight, h));
        root.style.width = w + 'px';
        root.style.height = h + 'px';
        active._w = w;
        active._h = h;
    }

    function onPointerUp() {
        window.removeEventListener('pointermove', onPointerMove);
        if (!active) return;
        root.classList.remove('sgc-resizable-active');
        const w = active._w ?? active.startW;
        const h = active._h ?? active.startH;
        active = null;
        try { dotnet.invokeMethodAsync('SetSize', w, h); } catch { /* noop */ }
    }

    root.addEventListener('pointerdown', onPointerDown);
    root._sgResizable = { onPointerDown, onPointerMove, onPointerUp };
}

export function detach(root) {
    if (!root || !root._sgResizable) return;
    const { onPointerDown, onPointerMove, onPointerUp } = root._sgResizable;
    root.removeEventListener('pointerdown', onPointerDown);
    window.removeEventListener('pointermove', onPointerMove);
    window.removeEventListener('pointerup', onPointerUp);
    window.removeEventListener('pointercancel', onPointerUp);
    delete root._sgResizable;
}
