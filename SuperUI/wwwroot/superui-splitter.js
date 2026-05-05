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
        const rect = first.getBoundingClientRect();
        const startX = e.clientX, startY = e.clientY;
        const startSize = vertical ? rect.height : rect.width;

        // Use pointer capture so move/up fire on bar even outside the window
        try { bar.setPointerCapture(e.pointerId); } catch { /* noop */ }

        const onMove = (ev) => {
            if (isDisposed || !dotnet) return;
            const delta = vertical ? (ev.clientY - startY) : (ev.clientX - startX);
            let next = startSize + delta;
            if (next < min) next = min;
            if (next > max) next = max;
            if (vertical) first.style.height = next + 'px';
            else first.style.width = next + 'px';
            try {
                dotnet.invokeMethodAsync('SetSize', next).catch(() => {});
            } catch { }
        };

        const onUp = () => {
            bar.removeEventListener('pointermove', onMove);
            bar.removeEventListener('pointerup', onUp);
            bar.removeEventListener('pointercancel', onUp);
        };

        bar.addEventListener('pointermove', onMove);
        bar.addEventListener('pointerup', onUp, { once: true });
        bar.addEventListener('pointercancel', onUp, { once: true });
    };

    bar._sgOnDown = onDown;
    bar._dispose = function() {
        isDisposed = true;
        dotnet = null;
    };
    bar.addEventListener('pointerdown', bar._sgOnDown);
}

export function detach(bar) {
    if (bar) {
        if (bar._dispose) {
            bar._dispose();
        }
        if (bar._sgOnDown) {
            bar.removeEventListener('pointerdown', bar._sgOnDown);
            delete bar._sgOnDown;
        }
    }
}
