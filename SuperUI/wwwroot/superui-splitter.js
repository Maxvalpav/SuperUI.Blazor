export function attach(bar, first, vertical, min, max, dotnet) {
    if (!bar) return;

    // Clean up existing listeners if any
    if (bar._sgOnDown) {
        bar.removeEventListener('pointerdown', bar._sgOnDown);
    }

    const onDown = (e) => {
        e.preventDefault();
        const rect = first.getBoundingClientRect();
        const startX = e.clientX, startY = e.clientY;
        const startSize = vertical ? rect.height : rect.width;
        bar.setPointerCapture?.(e.pointerId);

        const onMove = (ev) => {
            const delta = vertical ? (ev.clientY - startY) : (ev.clientX - startX);
            let next = startSize + delta;
            if (next < min) next = min;
            if (next > max) next = max;
            if (vertical) first.style.height = next + 'px';
            else first.style.width = next + 'px';
            dotnet.invokeMethodAsync('SetSize', next);
        };

        const onUp = () => {
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
        };

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp, { once: true });
    };

    bar._sgOnDown = onDown;
    bar.addEventListener('pointerdown', bar._sgOnDown);
}

export function detach(bar) {
    if (bar && bar._sgOnDown) {
        bar.removeEventListener('pointerdown', bar._sgOnDown);
        delete bar._sgOnDown;
    }
}
