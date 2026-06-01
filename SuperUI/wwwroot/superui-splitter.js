// ──────────────────────────────────────────────
// SuperUI Splitter — Multi-pane drag resize
// Supports simple 2-pane and N-pane modes
// ──────────────────────────────────────────────

function clamp(value, min, max) {
    if (value < min) return min;
    if (value > max) return max;
    return value;
}

function snap(value, grid) {
    if (!grid || grid <= 0) return value;
    return Math.round(value / grid) * grid;
}

// ─── Simple 2-pane mode ───

export function attach(bar, first, vertical, min, max, dotnet, disabled) {
    if (!bar || !first || disabled) return;

    let isDragging = false;
    let startX, startY, startSize, currentSize;

    function onPointerDown(e) {
        if (e.button !== 0) return;
        isDragging = true;
        bar.classList.add('active');
        try { dotnet?.invokeMethodAsync('SetDragging', true)?.catch(() => {}); } catch {}

        startX = e.clientX;
        startY = e.clientY;
        const rect = first.getBoundingClientRect();
        startSize = vertical ? rect.height : rect.width;
        currentSize = startSize;

        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup', onPointerUp);
        document.body.style.cursor = vertical ? 'row-resize' : 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
        bar.setPointerCapture(e.pointerId);
    }

    function onPointerMove(e) {
        if (!isDragging) return;
        const delta = vertical ? (e.clientY - startY) : (e.clientX - startX);
        currentSize = clamp(startSize + delta, min, max);

        if (vertical) first.style.height = currentSize + 'px';
        else first.style.width = currentSize + 'px';
    }

    function onPointerUp(e) {
        if (!isDragging) return;
        isDragging = false;
        bar.classList.remove('active');

        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        bar.releasePointerCapture(e.pointerId);

        try { dotnet?.invokeMethodAsync('SetSize', currentSize)?.catch(() => {}); } catch {}
        try { dotnet?.invokeMethodAsync('SetDragging', false)?.catch(() => {}); } catch {}
    }

    function onKeyDown(e) {
        let delta = 0;
        if (vertical) {
            if (e.key === 'ArrowUp') delta = -10;
            else if (e.key === 'ArrowDown') delta = 10;
        } else {
            if (e.key === 'ArrowLeft') delta = -10;
            else if (e.key === 'ArrowRight') delta = 10;
        }
        if (delta === 0) return;
        e.preventDefault();

        const rect = first.getBoundingClientRect();
        const curSize = vertical ? rect.height : rect.width;
        const newSize = clamp(curSize + delta, min, max);

        if (vertical) first.style.height = newSize + 'px';
        else first.style.width = newSize + 'px';

        try { dotnet?.invokeMethodAsync('SetSize', newSize)?.catch(() => {}); } catch {}
    }

    function onDoubleClick(e) {
        if (e.target === bar || e.target.closest('.sgc-split-handle')) {
            try { dotnet?.invokeMethodAsync('OnReset')?.catch(() => {}); } catch {}
        }
    }

    bar.addEventListener('pointerdown', onPointerDown);
    bar.addEventListener('dblclick', onDoubleClick);
    bar.addEventListener('keydown', onKeyDown);

    bar._sgSplitter = {
        dispose: () => {
            bar.removeEventListener('pointerdown', onPointerDown);
            bar.removeEventListener('dblclick', onDoubleClick);
            bar.removeEventListener('keydown', onKeyDown);
        }
    };
}

export function detach(bar) {
    if (bar && bar._sgSplitter) {
        bar._sgSplitter.dispose();
        delete bar._sgSplitter;
    }
}

// ─── Multi-pane mode ───

export function attachBars(barElements, paneElements, vertical, minSizes, maxSizes, initialSizes, dotnet, disabled, options) {
    if (!barElements || !paneElements || disabled) return;

    const opts = options || {};
    const bars = Array.isArray(barElements) ? barElements : [barElements];
    const panes = Array.isArray(paneElements) ? paneElements : [paneElements];
    const count = Math.min(bars.length, panes.length);
    if (count === 0) return;

    const sizes = initialSizes ? (Array.isArray(initialSizes) ? [...initialSizes] : [initialSizes]) : panes.map(() => 200);
    const minVals = Array.isArray(minSizes) ? minSizes : panes.map(() => 80);
    const maxVals = Array.isArray(maxSizes) ? maxSizes : panes.map(() => 1200);
    const step = opts.step || 10;

    const state = { isDragging: false, startX: 0, startY: 0, startSize: 0, currentSize: 0, activeIndex: -1 };

    function getSize(index) {
        if (index >= 0 && index < sizes.length) return sizes[index];
        return 0;
    }

    function setPaneSize(index, value) {
        if (index < 0 || index >= panes.length) return;
        const v = Math.max(0, value);
        sizes[index] = v;
        if (vertical) panes[index].style.height = v + 'px';
        else panes[index].style.width = v + 'px';
    }

    function initSizes() {
        for (let i = 0; i < panes.length; i++) {
            if (sizes[i] > 0) {
                setPaneSize(i, sizes[i]);
            }
        }
    }
    initSizes();

    function onPointerDown(e, index) {
        if (e.button !== 0) return;
        e.preventDefault();

        state.isDragging = true;
        state.activeIndex = index;
        state.startX = e.clientX;
        state.startY = e.clientY;

        const rect = panes[index].getBoundingClientRect();
        state.startSize = vertical ? rect.height : rect.width;
        state.currentSize = state.startSize;

        bars[index].classList.add('active');
        try { dotnet?.invokeMethodAsync('SetDragging', true)?.catch(() => {}); } catch {}

        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup', onPointerUp);
        document.body.style.cursor = vertical ? 'row-resize' : 'col-resize';
        document.body.style.userSelect = 'none';
        bars[index].setPointerCapture(e.pointerId);
    }

    function onPointerMove(e) {
        if (!state.isDragging || state.activeIndex < 0) return;
        const delta = vertical ? (e.clientY - state.startY) : (e.clientX - state.startX);
        const raw = state.startSize + delta;
        const snapped = snap(raw, opts.snapToGrid);
        state.currentSize = clamp(snapped, minVals[state.activeIndex] || 0, maxVals[state.activeIndex] || 9999);

        setPaneSize(state.activeIndex, state.currentSize);
    }

    function onPointerUp(e) {
        if (!state.isDragging) return;
        state.isDragging = false;

        if (state.activeIndex >= 0) {
            bars[state.activeIndex].classList.remove('active');
            bars[state.activeIndex].releasePointerCapture(e.pointerId);
        }

        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        try { dotnet?.invokeMethodAsync('SetSizes', sizes)?.catch(() => {}); } catch {}
        try { dotnet?.invokeMethodAsync('SetDragging', false)?.catch(() => {}); } catch {}

        state.activeIndex = -1;
    }

    function onKeyDown(e, index) {
        let delta = 0;
        if (vertical) {
            if (e.key === 'ArrowUp') delta = -step;
            else if (e.key === 'ArrowDown') delta = step;
        } else {
            if (e.key === 'ArrowLeft') delta = -step;
            else if (e.key === 'ArrowRight') delta = step;
        }
        if (delta === 0) return;
        e.preventDefault();

        const curSize = sizes[index] || 0;
        const raw = curSize + delta;
        const snapped = snap(raw, opts.snapToGrid);
        const newSize = clamp(snapped, minVals[index] || 0, maxVals[index] || 9999);

        setPaneSize(index, newSize);
        try { dotnet?.invokeMethodAsync('SetSizes', sizes)?.catch(() => {}); } catch {}
    }

    // Attach events to each bar
    for (let i = 0; i < count; i++) {
        const bar = bars[i];
        if (!bar) continue;

        const index = i;

        const pointerHandler = (e) => onPointerDown(e, index);
        const keyHandler = (e) => onKeyDown(e, index);

        bar.addEventListener('pointerdown', pointerHandler);
        bar.addEventListener('keydown', keyHandler);

        bar._sgSplitter = {
            dispose: () => {
                bar.removeEventListener('pointerdown', pointerHandler);
                bar.removeEventListener('keydown', keyHandler);
            }
        };
    }
}

export function detachBars(barElements) {
    const bars = Array.isArray(barElements) ? barElements : [barElements];
    for (const bar of bars) {
        if (bar && bar._sgSplitter) {
            bar._sgSplitter.dispose();
            delete bar._sgSplitter;
        }
    }
}
