const handlers = new WeakMap();

/**
 * Positions the menu relative to the root element based on placement.
 */
function positionMenu(root, menu, placement) {
    const r = root.getBoundingClientRect();
    const firstCol = menu.querySelector('.sgc-cascader-column');
    let menuW = menu.offsetWidth || 200;
    let menuH = menu.offsetHeight || 240;

    // Estimate menu width from first visible column if menu isn't fully rendered
    if (menuW < 50 && firstCol) {
        menuW = Math.max(r.width, 180);
    }

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

    // Clamp to viewport
    const maxLeft = window.innerWidth - menuW - 4;
    const maxTop = window.innerHeight - menuH - 4;
    if (left < 4) left = 4;
    if (top < 4) top = 4;
    if (left > maxLeft) left = maxLeft;
    if (top > maxTop) top = maxTop;

    menu.style.position = 'fixed';
    menu.style.left = left + 'px';
    menu.style.top = top + 'px';
    menu.style.minWidth = Math.max(r.width, 180) + 'px';
}

function getMenuById(root) {
    const menuId = root._sgCascaderMenuId;
    if (menuId) {
        return document.getElementById(menuId);
    }
    return root.querySelector('.sgc-cascader-menu');
}

export function attach(root, dotnetRef, placement, menuId) {
    detach(root);

    let isDisposed = false;
    let hoverTimer = null;

    root._sgCascaderMenuId = menuId;

    // ── Click outside ──
    const onPointerDown = (event) => {
        if (isDisposed) return;
        if (!root || root.contains(event.target)) return;
        const menu = getMenuById(root);
        if (menu && menu.contains(event.target)) return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    // ── Escape key ──
    const onKeyDown = (event) => {
        if (isDisposed || event.key !== "Escape") return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    // ── Reposition on scroll/resize ──
    const reposition = () => {
        if (isDisposed) return;
        const menu = getMenuById(root);
        if (!menu) return;
        positionMenu(root, menu, placement || 'BottomStart');
    };

    // ── Hover expand (delegated for performance) ──
    const onMouseOver = (event) => {
        if (isDisposed) return;
        const opt = event.target.closest('.sgc-cascader-option');
        if (!opt || opt.classList.contains('sgc-disabled') || opt.classList.contains('sgc-group-header')) return;
        try {
            dotnetRef?.invokeMethodAsync("HoverFromJsAsync", event.clientX, event.clientY)?.catch(() => {});
        } catch { }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("scroll", reposition, { passive: true, capture: true });
    window.addEventListener("resize", reposition);

    // Store for repositioning on menu open
    root._sgCascaderReposition = reposition;
    root._sgCascaderPlacement = placement;

    handlers.set(root, {
        onPointerDown,
        onKeyDown,
        reposition,
        onMouseOver,
        dispose: () => {
            isDisposed = true;
            dotnetRef = null;
            if (hoverTimer) clearTimeout(hoverTimer);
            root._sgCascaderReposition = null;
            root._sgCascaderMenuId = null;
        }
    });
}

export function detach(root) {
    const entry = handlers.get(root);
    if (!entry) return;

    if (entry.dispose) entry.dispose();
    document.removeEventListener("pointerdown", entry.onPointerDown);
    document.removeEventListener("keydown", entry.onKeyDown);
    window.removeEventListener("scroll", entry.reposition, true);
    window.removeEventListener("resize", entry.reposition);
    handlers.delete(root);
}

/**
 * Called from .NET after menu opens to position it.
 */
export function repositionMenu(root) {
    const menu = getMenuById(root);
    if (!menu) return;
    const placement = root._sgCascaderPlacement || 'BottomStart';
    menu.style.opacity = '0';
    positionMenu(root, menu, placement);
    requestAnimationFrame(() => {
        if (menu) menu.style.opacity = '';
    });
}
