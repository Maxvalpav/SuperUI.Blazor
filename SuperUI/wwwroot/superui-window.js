// Draggable & resizable floating window support.

export function attach(el, dotnetRef) {
    if (!el || el._sgWinAttached) return;
    el._sgWinAttached = true;

    el.addEventListener('pointerdown', () => {
        dotnetRef.invokeMethodAsync('FocusAsync');
    }, true);

    // Keyboard handlers (ESC to close, TAB for focus trap)
    el.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            dotnetRef.invokeMethodAsync('CloseAsync');
        }
        if (e.key === 'Tab') {
            const items = Array.from(el.querySelectorAll('button, a, input, select, textarea, [tabindex]:not([tabindex="-1"])'));
            if (items.length === 0) return;
            const first = items[0];
            const last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    });

    const header = el.querySelector('.sgc-win-header');
    if (header) {
        header.addEventListener('pointerdown', (e) => {
            if (e.target.closest('.sgc-win-btn')) return;
            if (el.classList.contains('sgc-win-maximized')) return;
            e.preventDefault();
            const rect = el.getBoundingClientRect();
            const offX = e.clientX - rect.left;
            const offY = e.clientY - rect.top;
            el.classList.add('sgc-win-dragging');
            const onMove = (ev) => {
                const nx = Math.max(0, Math.min(window.innerWidth - 40, ev.clientX - offX));
                const ny = Math.max(0, Math.min(window.innerHeight - 30, ev.clientY - offY));
                el.style.left = nx + 'px';
                el.style.top = ny + 'px';
                el.style.right = 'auto';
                el.style.bottom = 'auto';
            };
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                el.classList.remove('sgc-win-dragging');
            };
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        });
    }

    const handle = el.querySelector('.sgc-win-resize');
    if (handle) {
        handle.addEventListener('pointerdown', (e) => {
            if (el.classList.contains('sgc-win-maximized')) return;
            e.preventDefault();
            e.stopPropagation();
            const rect = el.getBoundingClientRect();
            const startW = rect.width;
            const startH = rect.height;
            const startX = e.clientX;
            const startY = e.clientY;
            el.classList.add('sgc-win-resizing');
            const onMove = (ev) => {
                const nw = Math.max(180, startW + (ev.clientX - startX));
                const nh = Math.max(100, startH + (ev.clientY - startY));
                el.style.width = nw + 'px';
                el.style.height = nh + 'px';
            };
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                el.classList.remove('sgc-win-resizing');
            };
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        });
    }
}
