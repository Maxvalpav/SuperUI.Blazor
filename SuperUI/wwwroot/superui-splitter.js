export function attach(bar, first, vertical, min, max, dotnet, disabled) {
    if (!bar || !first || disabled) return;

    let isDragging = false;
    let startX, startY, startSize;

    const onMouseDown = (e) => {
        if (e.button !== 0) return;
        isDragging = true;
        bar.classList.add('active');
        dotnet.invokeMethodAsync('SetDragging', true);
        
        startX = e.clientX;
        startY = e.clientY;
        const rect = first.getBoundingClientRect();
        startSize = vertical ? rect.height : rect.width;
        currentSize = startSize;

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
        document.body.style.cursor = vertical ? 'row-resize' : 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    };

    let currentSize = 0;

    const clamp = (value) => {
        if (value < min && value > min / 2) return min;
        if (value <= min / 2) return 0;
        if (value > max) return max;
        return value;
    };

    const onMouseMove = (e) => {
        if (!isDragging) return;
        const delta = vertical ? (e.clientY - startY) : (e.clientX - startX);
        currentSize = clamp(startSize + delta);
        
        // Update DOM directly — no Blazor re-render during drag
        if (vertical) first.style.height = currentSize + 'px';
        else          first.style.width  = currentSize + 'px';
    };

    const onMouseUp = async () => {
        if (!isDragging) return;
        isDragging = false;
        bar.classList.remove('active');
        
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // Notify Blazor only once after drag ends — size first, then dragging state
        await dotnet.invokeMethodAsync('SetSize', currentSize);
        dotnet.invokeMethodAsync('SetDragging', false);
    };

    const onDoubleClick = (e) => {
        // Only reset if clicked directly on the bar, not on buttons
        if (e.target === bar || e.target.classList.contains('sgc-split-handle')) {
            dotnet.invokeMethodAsync('OnReset');
        }
    };

    bar.addEventListener('mousedown', onMouseDown);
    bar.addEventListener('dblclick', onDoubleClick);

    // Touch support
    const onTouchStart = (e) => {
        if (e.touches.length !== 1) return;
        // Don't drag if clicking on collapse buttons
        if (e.target.closest('.sgc-split-collapse-btn')) return;

        isDragging = true;
        bar.classList.add('active');
        dotnet.invokeMethodAsync('SetDragging', true);
        
        const touch = e.touches[0];
        startX = touch.clientX;
        startY = touch.clientY;
        const rect = first.getBoundingClientRect();
        startSize = vertical ? rect.height : rect.width;
        currentSize = startSize;

        document.addEventListener('touchmove', onTouchMove, { passive: false });
        document.addEventListener('touchend', onTouchEnd);
        // e.preventDefault(); // Removed to allow clicks on buttons
    };

    const onTouchMove = (e) => {
        if (!isDragging || e.touches.length !== 1) return;
        const touch = e.touches[0];
        const delta = vertical ? (touch.clientY - startY) : (touch.clientX - startX);
        currentSize = clamp(startSize + delta);

        // Update DOM directly — no Blazor re-render during drag
        if (vertical) first.style.height = currentSize + 'px';
        else          first.style.width  = currentSize + 'px';

        e.preventDefault();
    };

    const onTouchEnd = async () => {
        if (!isDragging) return;
        isDragging = false;
        bar.classList.remove('active');
        document.removeEventListener('touchmove', onTouchMove);
        document.removeEventListener('touchend', onTouchEnd);

        // Notify Blazor only once after drag ends — size first, then dragging state
        await dotnet.invokeMethodAsync('SetSize', currentSize);
        dotnet.invokeMethodAsync('SetDragging', false);
    };

    bar.addEventListener('touchstart', onTouchStart, { passive: false });

    bar._sgSplitter = {
        dispose: () => {
            bar.removeEventListener('mousedown', onMouseDown);
            bar.removeEventListener('dblclick', onDoubleClick);
            bar.removeEventListener('touchstart', onTouchStart);
        }
    };
}

export function detach(bar) {
    if (bar && bar._sgSplitter) {
        bar._sgSplitter.dispose();
        delete bar._sgSplitter;
    }
}
