// SgDockManager — pointer-based sash (splitter) resize.
// Three sashes: left (vertical), right (vertical), bottom (horizontal).

export function attach(root, dotnet) {
    if (!root) return;
    if (root._sgDock) detach(root);

    let isDisposed = false;

    const state = {
        dispose() { isDisposed = true; dotnet = null; }
    };

    function invoke(method, value) {
        if (isDisposed || !dotnet) return;
        try { dotnet.invokeMethodAsync(method, value).catch(() => {}); } catch { /* noop */ }
    }

    // ── Sash drag ────────────────────────────────────────────────────────────

    function onSashDown(e) {
        if (isDisposed || !dotnet || e.button !== 0) return;
        const sash = e.currentTarget;
        const kind = sash.getAttribute('data-sash'); // 'left' | 'right' | 'bottom'
        if (!kind) return;
        e.preventDefault();

        const isVertical = kind === 'left' || kind === 'right';
        const startPos = isVertical ? e.clientX : e.clientY;

        // Measure the adjacent panel
        let panel = null;
        if (kind === 'left')   panel = root.querySelector('.sgc-dock-left');
        if (kind === 'right')  panel = root.querySelector('.sgc-dock-right');
        if (kind === 'bottom') panel = root.querySelector('.sgc-dock-bottom');

        if (!panel) return;

        const startSize = isVertical ? panel.getBoundingClientRect().width
                                     : panel.getBoundingClientRect().height;

        try { sash.setPointerCapture(e.pointerId); } catch { /* noop */ }
        sash.classList.add('sgc-dock-sash-active');

        const onMove = (ev) => {
            if (isDisposed) return;
            const delta = isVertical ? (ev.clientX - startPos) : (ev.clientY - startPos);
            let next = kind === 'right' || kind === 'bottom'
                ? startSize - delta
                : startSize + delta;
            next = Math.max(60, next);

            if (isVertical) panel.style.width  = next + 'px';
            else            panel.style.height = next + 'px';
        };

        const onUp = () => {
            window.removeEventListener('pointermove', onMove);
            sash.classList.remove('sgc-dock-sash-active');
            if (isDisposed || !dotnet) return;

            const finalSize = isVertical
                ? panel.getBoundingClientRect().width
                : panel.getBoundingClientRect().height;

            const method = kind === 'left'   ? 'JsResizeLeft'
                         : kind === 'right'  ? 'JsResizeRight'
                         :                    'JsResizeBottom';
            invoke(method, finalSize);
        };

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp, { once: true });
        window.addEventListener('pointercancel', onUp, { once: true });
    }

    // Attach to all sashes inside root
    function bindSashes() {
        root.querySelectorAll('.sgc-dock-sash').forEach(s => {
            s.removeEventListener('pointerdown', onSashDown);
            s.addEventListener('pointerdown', onSashDown);
        });
    }

    // Use MutationObserver so newly rendered sashes get bound too
    const observer = new MutationObserver(() => bindSashes());
    observer.observe(root, { childList: true, subtree: true });
    bindSashes();

    root._sgDock = { state, observer };
}

export function detach(root) {
    if (!root || !root._sgDock) return;
    const { state, observer } = root._sgDock;
    state?.dispose();
    observer?.disconnect();
    delete root._sgDock;
}
