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

    // ── Tab Drag & Drop ──────────────────────────────────────────────────────

    function onDragStart(e) {
        if (isDisposed) return;
        const tab = e.target.closest('.sgc-dock-tab');
        if (!tab) return;
        const panelId = tab.getAttribute('data-panel-id');
        if (!panelId) return;

        e.dataTransfer.setData('text/plain', panelId);
        e.dataTransfer.effectAllowed = 'move';
        tab.classList.add('sgc-dock-tab-dragging');
        
        // Use a delay to allow the ghost image to be created before we hide/fade the original
        setTimeout(() => tab.style.opacity = '0.4', 0);
    }

    function onDragEnd(e) {
        const tab = e.target.closest('.sgc-dock-tab');
        if (tab) {
            tab.classList.remove('sgc-dock-tab-dragging');
            tab.style.opacity = '';
        }
        root.querySelectorAll('.sgc-dock-tabbar').forEach(tb => {
            tb.classList.remove('sgc-dock-tabbar-drop-target');
        });
    }

    function onDragOver(e) {
        if (isDisposed) return;
        const tabbar = e.target.closest('.sgc-dock-tabbar');
        if (!tabbar) return;

        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        
        root.querySelectorAll('.sgc-dock-tabbar').forEach(tb => {
            tb.classList.remove('sgc-dock-tabbar-drop-target');
        });
        tabbar.classList.add('sgc-dock-tabbar-drop-target');
    }

    function onDragLeave(e) {
        const tabbar = e.target.closest('.sgc-dock-tabbar');
        if (tabbar) {
            tabbar.classList.remove('sgc-dock-tabbar-drop-target');
        }
    }

    function onDrop(e) {
        if (isDisposed || !dotnet) return;
        const tabbar = e.target.closest('.sgc-dock-tabbar');
        if (!tabbar) return;

        e.preventDefault();
        tabbar.classList.remove('sgc-dock-tabbar-drop-target');

        const panelId = e.dataTransfer.getData('text/plain');
        const targetPos = tabbar.getAttribute('data-pos');

        if (panelId && targetPos) {
            invoke('JsMovePanel', panelId, targetPos);
        }
    }

    // Attach to sashes and tabs
    function bindAll() {
        root.querySelectorAll('.sgc-dock-sash').forEach(s => {
            s.removeEventListener('pointerdown', onSashDown);
            s.addEventListener('pointerdown', onSashDown);
        });

        root.querySelectorAll('.sgc-dock-tab').forEach(t => {
            t.removeEventListener('dragstart', onDragStart);
            t.addEventListener('dragstart', onDragStart);
            t.removeEventListener('dragend', onDragEnd);
            t.addEventListener('dragend', onDragEnd);
        });

        root.querySelectorAll('.sgc-dock-tabbar').forEach(tb => {
            tb.removeEventListener('dragover', onDragOver);
            tb.addEventListener('dragover', onDragOver);
            tb.removeEventListener('dragleave', onDragLeave);
            tb.addEventListener('dragleave', onDragLeave);
            tb.removeEventListener('drop', onDrop);
            tb.addEventListener('drop', onDrop);
        });
    }

    // Use MutationObserver so newly rendered sashes/tabs get bound too
    const observer = new MutationObserver(() => bindAll());
    observer.observe(root, { childList: true, subtree: true });
    bindAll();

    root._sgDock = { state, observer };
}

export function detach(root) {
    if (!root || !root._sgDock) return;
    const { state, observer } = root._sgDock;
    state?.dispose();
    observer?.disconnect();
    delete root._sgDock;
}
