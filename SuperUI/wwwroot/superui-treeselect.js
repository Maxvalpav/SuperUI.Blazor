const handlers = new WeakMap();

function positionMenu(root, menu, placement) {
    const r = root.getBoundingClientRect();
    let menuW = menu.offsetWidth || 220;
    let menuH = menu.offsetHeight || 240;

    if (menuW < 60) menuW = Math.max(r.width, 200);

    const gap = 4;
    let top, left;

    switch (placement) {
        case 'BottomStart':
        default:
            top = r.bottom + gap;
            left = r.left;
            break;
        case 'BottomEnd':
            top = r.bottom + gap;
            left = r.right - menuW;
            break;
        case 'TopStart':
            top = r.top - menuH - gap;
            left = r.left;
            break;
        case 'TopEnd':
            top = r.top - menuH - gap;
            left = r.right - menuW;
            break;
    }

    const maxLeft = window.innerWidth - menuW - 4;
    const maxTop = window.innerHeight - menuH - 4;
    if (left < 4) left = 4;
    if (top < 4) top = 4;
    if (left > maxLeft) left = maxLeft;
    if (top > maxTop) top = maxTop;

    menu.style.position = 'fixed';
    menu.style.left = left + 'px';
    menu.style.top = top + 'px';
    menu.style.minWidth = Math.max(r.width, 200) + 'px';
}

export function attach(root, dotnetRef, placement) {
    detach(root);

    let isDisposed = false;

    const onPointerDown = (event) => {
        if (isDisposed) return;
        if (!root || root.contains(event.target)) return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch {}
    };

    const onKeyDown = (event) => {
        if (isDisposed || event.key !== "Escape") return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch {}
    };

    const reposition = () => {
        if (isDisposed) return;
        const menu = root.querySelector('.sgc-treeselect-menu');
        if (!menu) return;
        positionMenu(root, menu, placement || 'BottomStart');
    };

    const onResize = () => reposition();
    const onScroll = () => reposition();

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("scroll", onScroll, true);
    window.addEventListener("resize", onResize);

    root._sgTreeSelectReposition = reposition;
    root._sgTreeSelectPlacement = placement;

    handlers.set(root, {
        onPointerDown,
        onKeyDown,
        reposition,
        onResize,
        onScroll,
        dispose: () => {
            isDisposed = true;
            dotnetRef = null;
            root._sgTreeSelectReposition = null;
        }
    });
}

export function detach(root) {
    const entry = handlers.get(root);
    if (!entry) return;
    if (entry.dispose) entry.dispose();
    document.removeEventListener("pointerdown", entry.onPointerDown);
    document.removeEventListener("keydown", entry.onKeyDown);
    window.removeEventListener("scroll", entry.onScroll, true);
    window.removeEventListener("resize", entry.onResize);
    handlers.delete(root);
}

export function repositionMenu(root) {
    const menu = root.querySelector('.sgc-treeselect-menu');
    if (!menu) return;
    const placement = root._sgTreeSelectPlacement || 'BottomStart';
    positionMenu(root, menu, placement);
}
