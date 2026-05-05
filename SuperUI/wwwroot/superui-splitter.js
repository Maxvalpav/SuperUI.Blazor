export function attach(bar, first, vertical, min, max, dotnet) {
    if (!bar) return;

    // Clean up existing listeners if any
    if (bar._sgOnDown) {
        bar.removeEventListener('pointerdown', bar._sgOnDown);
    }

    let isDisposed = false;

    const onDown = (e) => {
        if (isDisposed || !dotnet) return;
        if (e.button !== 0) return;
        e.preventDefault();

        const rect     = first.getBoundingClientRect();
        const startX   = e.clientX;
        const startY   = e.clientY;
        const startSize = vertical ? rect.height : rect.width;

        const onMove = (ev) => {
            if (isDisposed || !dotnet) return;
            const delta = vertical ? (ev.clientY - startY) : (ev.clientX - startX);
            let next = startSize + delta;
            if (next < min) next = min;
            if (next > max) next = max;
            if (vertical) first.style.height = next + 'px';
            else          first.style.width  = next + 'px';
            try { dotnet.invokeMethodAsync('SetSize', next).catch(() => {}); } catch { /* noop */ }
        };

        const onUp = () => {
            document.removeEventListener('pointermove',   onMove);
            document.removeEventListener('pointerup',     onUp);
            document.removeEventListener('pointercancel', onUp);
        };

        // Listen on document so events keep firing even when cursor leaves the bar
        document.addEventListener('pointermove',   onMove);
        document.addEventListener('pointerup',     onUp, { once: true });
        document.addEventListener('pointercancel', onUp, { once: true });
    };

    bar._sgOnDown = onDown;
    bar._dispose  = function () { isDisposed = true; dotnet = null; };
    bar.addEventListener('pointerdown', bar._sgOnDown);
}

export function detach(bar) {
    if (!bar) return;
    if (bar._dispose)   bar._dispose();
    if (bar._sgOnDown)  bar.removeEventListener('pointerdown', bar._sgOnDown);
    delete bar._sgOnDown;
    delete bar._dispose;
}
