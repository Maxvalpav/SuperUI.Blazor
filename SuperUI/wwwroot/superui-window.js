// Draggable & resizable floating window support.

const SNAP_THRESHOLD = 15;
const ARROW_KEY_STEP = 10;

// WeakMap<el, cleanup()> — lets detach() remove all listeners precisely.
const _cleanup = new WeakMap();

export function attach(el, dotnetRef) {
    if (!el || el._sgWinAttached) return;
    el._sgWinAttached = true;

    let isDisposed = false;

    const invoke = (method, ...args) => {
        if (isDisposed || !dotnetRef) return;
        try {
            dotnetRef.invokeMethodAsync(method, ...args).catch(() => {});
        } catch { }
    };

    // Clamp initial position into viewport.
    if (!el.classList.contains('sgc-win-maximized')) {
        const r = el.getBoundingClientRect();
        if (r.width > 0 && r.height > 0) {
            const maxL = Math.max(0, window.innerWidth  - r.width);
            const maxT = Math.max(0, window.innerHeight - r.height);
            const cl = Math.max(0, Math.min(maxL, r.left));
            const ct = Math.max(0, Math.min(maxT, r.top));
            if (cl !== r.left || ct !== r.top) {
                el.style.left = cl + 'px';
                el.style.top  = ct + 'px';
                invoke('UpdateBoundsAsync', cl, ct, r.width, r.height);
            }
        }
    }

    const onFocusDown = () => invoke('FocusAsync');
    el.addEventListener('pointerdown', onFocusDown, true);

    const isEditable = () => {
        const a = document.activeElement;
        if (!a) return false;
        if (['INPUT', 'TEXTAREA', 'SELECT'].includes(a.tagName)) return true;
        const ce = a.getAttribute && a.getAttribute('contenteditable');
        return ce && ce !== 'false';
    };

    const onKeyDown = (e) => {
        if (isDisposed || !dotnetRef) return;
        
        if (e.ctrlKey && e.key === 'F4') {
            e.preventDefault();
            invoke('CloseAsync');
            return;
        }
        if (e.key === 'Escape' && !isEditable()) {
            invoke('CloseAsync');
            return;
        }
        if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.key)
            && !el.classList.contains('sgc-win-maximized') && !isEditable()) {
            e.preventDefault();
            const rect = el.getBoundingClientRect();
            let left = parseFloat(el.style.left) || rect.left;
            let top  = parseFloat(el.style.top)  || rect.top;
            if (e.key === 'ArrowUp')    top  -= ARROW_KEY_STEP;
            if (e.key === 'ArrowDown')  top  += ARROW_KEY_STEP;
            if (e.key === 'ArrowLeft')  left -= ARROW_KEY_STEP;
            if (e.key === 'ArrowRight') left += ARROW_KEY_STEP;
            left = Math.max(0, Math.min(window.innerWidth  - rect.width,  left));
            top  = Math.max(0, Math.min(window.innerHeight - rect.height, top));
            el.style.left = left + 'px';
            el.style.top  = top  + 'px';
            el.style.right = el.style.bottom = 'auto';
            invoke('UpdateBoundsAsync', left, top, rect.width, rect.height);
        }
        if (e.key === 'Tab') {
            const items = Array.from(el.querySelectorAll(
                'button, a, input, select, textarea, [tabindex]:not([tabindex="-1"])'));
            if (!items.length) return;
            const first = items[0], last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    };
    el.addEventListener('keydown', onKeyDown);

    const header = el.querySelector('.sgc-win-header');
    if (header) {
        const onDblClick = (e) => {
            if (isDisposed || !dotnetRef) return;
            if (e.target.closest('.sgc-win-btn')) return;
            if (el.classList.contains('sgc-win-minimized')) {
                invoke('MinimizeAsync');
                return;
            }
            invoke('ToggleMaximizeAsync');
        };
        header.addEventListener('dblclick', onDblClick);

        const onDragStart = (e) => {
            if (isDisposed || !dotnetRef) return;
            if (e.button !== 0) return;
            if (e.target.closest('.sgc-win-btn')) return;
            if (el.classList.contains('sgc-win-maximized')) return;
            if (el.classList.contains('sgc-win-minimized')) return;
            e.preventDefault();
            const rect = el.getBoundingClientRect();
            const offX = e.clientX - rect.left;
            const offY = e.clientY - rect.top;
            el.classList.add('sgc-win-dragging');

            let pendingX = rect.left, pendingY = rect.top, rafId = 0;
            const apply = () => {
                rafId = 0;
                el.style.left = pendingX + 'px';
                el.style.top  = pendingY + 'px';
                el.style.right = el.style.bottom = 'auto';
            };
            const onMove = (ev) => {
                if (isDisposed || !dotnetRef) return;
                const ww = window.innerWidth, wh = window.innerHeight;
                let nx = ev.clientX - offX, ny = ev.clientY - offY;

                // --- Improved Snapping Logic ---
                let bestSnapX = nx;
                let minDistX = SNAP_THRESHOLD;
                let bestSnapY = ny;
                let minDistY = SNAP_THRESHOLD;

                // Viewport edges
                if (Math.abs(nx) < minDistX) { bestSnapX = 0; minDistX = Math.abs(nx); }
                if (Math.abs(nx + rect.width - ww) < minDistX) { bestSnapX = ww - rect.width; minDistX = Math.abs(nx + rect.width - ww); }
                if (Math.abs(ny) < minDistY) { bestSnapY = 0; minDistY = Math.abs(ny); }
                if (Math.abs(ny + rect.height - wh) < minDistY) { bestSnapY = wh - rect.height; minDistY = Math.abs(ny + rect.height - wh); }

                // Other windows
                const others = document.querySelectorAll('.sgc-win:not(.sgc-win-dragging):not(.sgc-win-maximized)');
                for (const other of others) {
                    if (other === el) continue;
                    const o = other.getBoundingClientRect();
                    
                    // Check if windows overlap in Y to snap in X
                    const overlapY = (ny < o.bottom + SNAP_THRESHOLD) && (ny + rect.height > o.top - SNAP_THRESHOLD);
                    if (overlapY) {
                        // Snap left to right
                        if (Math.abs(nx - o.right) < minDistX) { bestSnapX = o.right; minDistX = Math.abs(nx - o.right); }
                        // Snap right to left
                        if (Math.abs(nx + rect.width - o.left) < minDistX) { bestSnapX = o.left - rect.width; minDistX = Math.abs(nx + rect.width - o.left); }
                        // Snap left to left
                        if (Math.abs(nx - o.left) < minDistX) { bestSnapX = o.left; minDistX = Math.abs(nx - o.left); }
                        // Snap right to right
                        if (Math.abs(nx + rect.width - o.right) < minDistX) { bestSnapX = o.right - rect.width; minDistX = Math.abs(nx + rect.width - o.right); }
                    }

                    // Check if windows overlap in X to snap in Y
                    const overlapX = (nx < o.right + SNAP_THRESHOLD) && (nx + rect.width > o.left - SNAP_THRESHOLD);
                    if (overlapX) {
                        // Snap top to bottom
                        if (Math.abs(ny - o.bottom) < minDistY) { bestSnapY = o.bottom; minDistY = Math.abs(ny - o.bottom); }
                        // Snap bottom to top
                        if (Math.abs(ny + rect.height - o.top) < minDistY) { bestSnapY = o.top - rect.height; minDistY = Math.abs(ny + rect.height - o.top); }
                        // Snap top to top
                        if (Math.abs(ny - o.top) < minDistY) { bestSnapY = o.top; minDistY = Math.abs(ny - o.top); }
                        // Snap bottom to bottom
                        if (Math.abs(ny + rect.height - o.bottom) < minDistY) { bestSnapY = o.bottom - rect.height; minDistY = Math.abs(ny + rect.height - o.bottom); }
                    }
                }

                nx = bestSnapX;
                ny = bestSnapY;

                pendingX = nx; pendingY = ny;
                if (!rafId) rafId = requestAnimationFrame(apply);
            };
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                if (rafId) { cancelAnimationFrame(rafId); apply(); }
                el.classList.remove('sgc-win-dragging');
                if (!isDisposed && dotnetRef) {
                    const fr = el.getBoundingClientRect();
                    invoke('UpdateBoundsAsync', parseFloat(el.style.left), parseFloat(el.style.top), fr.width, fr.height);
                }
            };
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        };
        header.addEventListener('pointerdown', onDragStart);
    }

    const handle = el.querySelector('.sgc-win-resize');
    if (handle) {
        const onResizeStart = (e) => {
            if (isDisposed || !dotnetRef) return;
            if (e.button !== 0) return;
            if (el.classList.contains('sgc-win-maximized')) return;
            e.preventDefault(); e.stopPropagation();
            const rect = el.getBoundingClientRect();
            const startW = rect.width, startH = rect.height;
            const startX = e.clientX, startY = e.clientY;
            el.classList.add('sgc-win-resizing');
            let pendingW = startW, pendingH = startH, rafId = 0;
            const apply = () => {
                rafId = 0;
                el.style.width  = pendingW + 'px';
                el.style.height = pendingH + 'px';
            };
            const onMove = (ev) => {
                if (isDisposed || !dotnetRef) return;
                const left = parseFloat(el.style.left) || rect.left;
                const top  = parseFloat(el.style.top)  || rect.top;
                pendingW = Math.max(180, Math.min(window.innerWidth  - left, startW + (ev.clientX - startX)));
                pendingH = Math.max(100, Math.min(window.innerHeight - top,  startH + (ev.clientY - startY)));
                if (!rafId) rafId = requestAnimationFrame(apply);
            };
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                if (rafId) { cancelAnimationFrame(rafId); apply(); }
                el.classList.remove('sgc-win-resizing');
                if (!isDisposed && dotnetRef) {
                    const fr = el.getBoundingClientRect();
                    invoke('UpdateBoundsAsync', parseFloat(el.style.left), parseFloat(el.style.top), fr.width, fr.height);
                }
            };
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        };
        handle.addEventListener('pointerdown', onResizeStart);
    }

    _cleanup.set(el, () => {
        isDisposed = true;
        dotnetRef = null;
        el.removeEventListener('pointerdown', onFocusDown, true);
        el.removeEventListener('keydown', onKeyDown);
        el._sgWinAttached = false;
    });
}

export function detach(el) {
    if (!el) return;
    const cleanup = _cleanup.get(el);
    if (cleanup) { cleanup(); _cleanup.delete(el); }
}
