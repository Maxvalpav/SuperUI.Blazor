// ──────────────────────────────────────────────────────────────────────────
// SuperUI Splitter — unified drag-resize engine
//
// A single code path handles every layout: two-pane (First/Second) is just a
// one-bar / two-pane case of the N-pane model. This guarantees identical drag,
// keyboard, double-click-reset and ARIA behaviour everywhere.
//
// Design notes:
//   * Pointer events → works for mouse, touch and pen with one handler set.
//   * No per-move .NET round-trips: the DOM is mutated directly during drag and
//     a single SetSizes(...) is dispatched on release (keyboard dispatches per key).
//   * aria-valuenow is updated live so assistive tech and automated agents can
//     observe the current pane size during interaction.
//   * Dependency-free; safe to import in WASM and Server circuits.
// ──────────────────────────────────────────────────────────────────────────

function clamp(value, min, max) {
    if (min != null && value < min) return min;
    if (max != null && value > max) return max;
    return value;
}

function snap(value, grid) {
    if (!grid || grid <= 0) return value;
    return Math.round(value / grid) * grid;
}

function dispatch(dotnet, method, ...args) {
    try { dotnet?.invokeMethodAsync(method, ...args)?.catch(() => {}); } catch { /* circuit gone */ }
}

/**
 * Attach the resize engine to a set of bars and panes.
 * The disposer is stored on the root element so detach(root) can tear it down
 * without the caller having to retain a JS object reference across interop.
 * @param {HTMLElement} root     splitter container element (holds the handle)
 * @param {HTMLElement[]} bars   one bar per resizable boundary (count = panes.length - 1)
 * @param {HTMLElement[]} panes  every pane element; the last pane flexes and has no bar
 * @param {object} dotnet        DotNetObjectReference for [JSInvokable] callbacks
 * @param {object} opts          { vertical, step, snapToGrid, keyboardResize, disabled, mins[], maxs[], sizes[] }
 */
export function attach(root, bars, panes, dotnet, opts) {
    opts = opts || {};
    bars = Array.isArray(bars) ? bars.filter(Boolean) : (bars ? [bars] : []);
    panes = Array.isArray(panes) ? panes.filter(Boolean) : (panes ? [panes] : []);
    if (!root) return;
    detach(root); // idempotent re-attach when pane count changes
    if (bars.length === 0 || panes.length < 2) return;

    const vertical = !!opts.vertical;
    const step = opts.step > 0 ? opts.step : 10;
    const grid = opts.snapToGrid;
    const keyboard = opts.keyboardResize !== false;
    // constrained=false lets a pane be dragged past its Min/Max so it can shrink to
    // nothing (overlapping its content, which then scrolls) or grow up to the
    // container edge. Min/Max are still reported via ARIA as the *recommended* range.
    const constrained = opts.constrained !== false;
    const mins = opts.mins || [];
    const maxs = opts.maxs || [];
    const sizes = (opts.sizes ? [...opts.sizes] : []);

    function containerExtent() {
        const rect = root.getBoundingClientRect();
        return vertical ? rect.height : rect.width;
    }

    const minOf = i => constrained ? (mins[i] != null ? mins[i] : 0) : 0;
    const maxOf = i => constrained
        ? (maxs[i] != null ? maxs[i] : Number.MAX_SAFE_INTEGER)
        : Math.max(0, containerExtent());

    // Apply the authoritative sizes coming from .NET so SSR markup and JS agree.
    function applySize(i, value) {
        const v = Math.max(0, value);
        sizes[i] = v;
        if (vertical) panes[i].style.height = v + 'px';
        else panes[i].style.width = v + 'px';
        const bar = bars[i];
        if (bar) bar.setAttribute('aria-valuenow', Math.round(v).toString());
    }
    for (let i = 0; i < bars.length; i++) {
        if (sizes[i] != null) applySize(i, sizes[i]);
    }

    const state = { dragging: false, index: -1, start: 0, origin: 0 };
    let disabled = !!opts.disabled;

    function measure(i) {
        const rect = panes[i].getBoundingClientRect();
        return vertical ? rect.height : rect.width;
    }

    function onPointerDown(e, index) {
        if (disabled || e.button !== 0) return;
        e.preventDefault();
        state.dragging = true;
        state.index = index;
        state.origin = vertical ? e.clientY : e.clientX;
        state.start = measure(index);

        bars[index].classList.add('active');
        try { bars[index].setPointerCapture(e.pointerId); } catch { /* ignore */ }
        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup', onPointerUp);
        document.body.style.cursor = vertical ? 'row-resize' : 'col-resize';
        document.body.style.userSelect = 'none';

        dispatch(dotnet, 'ResizeStart');
        dispatch(dotnet, 'SetDragging', true);
    }

    function onPointerMove(e) {
        if (!state.dragging || state.index < 0) return;
        const i = state.index;
        const delta = (vertical ? e.clientY : e.clientX) - state.origin;
        const next = clamp(snap(state.start + delta, grid), minOf(i), maxOf(i));
        applySize(i, next);
    }

    function endDrag(e) {
        if (!state.dragging) return;
        const i = state.index;
        state.dragging = false;
        state.index = -1;

        if (bars[i]) {
            bars[i].classList.remove('active');
            if (e) { try { bars[i].releasePointerCapture(e.pointerId); } catch { /* ignore */ } }
        }
        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        dispatch(dotnet, 'SetSizes', sizes);
        dispatch(dotnet, 'SetDragging', false);
    }

    function onPointerUp(e) { endDrag(e); }

    function onKeyDown(e, index) {
        if (disabled || !keyboard) return;
        let delta = 0;
        let absolute = null;
        const dec = vertical ? 'ArrowUp' : 'ArrowLeft';
        const inc = vertical ? 'ArrowDown' : 'ArrowRight';
        if (e.key === dec) delta = -step;
        else if (e.key === inc) delta = step;
        else if (e.key === 'Home') absolute = minOf(index);
        else if (e.key === 'End') absolute = maxOf(index);
        else return;

        e.preventDefault();
        const current = sizes[index] != null ? sizes[index] : measure(index);
        const raw = absolute != null ? absolute : current + delta;
        applySize(index, clamp(snap(raw, grid), minOf(index), maxOf(index)));
        dispatch(dotnet, 'SetSizes', sizes);
    }

    function onDoubleClick() {
        if (disabled) return;
        dispatch(dotnet, 'Reset');
    }

    const cleanups = [];
    bars.forEach((bar, i) => {
        const down = e => onPointerDown(e, i);
        const key = e => onKeyDown(e, i);
        bar.addEventListener('pointerdown', down);
        bar.addEventListener('keydown', key);
        bar.addEventListener('dblclick', onDoubleClick);
        cleanups.push(() => {
            bar.removeEventListener('pointerdown', down);
            bar.removeEventListener('keydown', key);
            bar.removeEventListener('dblclick', onDoubleClick);
        });
    });

    root._sgSplitter = {
        setSizes(next) {
            if (!Array.isArray(next)) return;
            for (let i = 0; i < bars.length && i < next.length; i++) {
                if (next[i] != null) applySize(i, next[i]);
            }
        },
        setDisabled(value) { disabled = !!value; },
        dispose() {
            endDrag(null);
            cleanups.forEach(fn => fn());
            cleanups.length = 0;
        }
    };
}

/** Push authoritative sizes from .NET into the live DOM (e.g. after collapse/reset). */
export function setSizes(root, sizes) {
    root?._sgSplitter?.setSizes(sizes);
}

/** Toggle the disabled flag without re-attaching. */
export function setDisabled(root, value) {
    root?._sgSplitter?.setDisabled(value);
}

export function detach(root) {
    if (root && root._sgSplitter) {
        root._sgSplitter.dispose();
        delete root._sgSplitter;
    }
}
