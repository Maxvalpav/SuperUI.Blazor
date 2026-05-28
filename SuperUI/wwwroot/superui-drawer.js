// superui-drawer.js — Focus-trap, ESC, scroll-lock, resize with min/max

const drawerStack = [];
let escapeHandler = null;
let focusTrapHandler = null;
let previousScrollPosition = 0;

function getTopDrawer() {
    return drawerStack[drawerStack.length - 1];
}

function getFocusableElements(element) {
    return Array.from(element.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
    ));
}

function getScrollbarWidth() {
    const outer = document.createElement('div');
    outer.style.visibility = 'hidden';
    outer.style.overflow = 'scroll';
    document.body.appendChild(outer);
    const inner = document.createElement('div');
    outer.appendChild(inner);
    const w = outer.offsetWidth - inner.offsetWidth;
    outer.parentNode.removeChild(outer);
    return w;
}

export function attach(drawerElement, dotnetRef, closeOnEscape) {
    const previousFocus = document.activeElement;

    if (drawerStack.length === 0) {
        previousScrollPosition = window.scrollY || document.documentElement.scrollTop;
        document.body.style.overflow = 'hidden';
        document.body.style.paddingRight = getScrollbarWidth() + 'px';
    }

    const entry = {
        element: drawerElement,
        dotnet: dotnetRef,
        closeOnEscape,
        previousFocus,
        isDisposed: false,
        dispose: () => {
            entry.isDisposed = true;
            entry.dotnet = null;
        }
    };

    drawerStack.push(entry);

    if (!escapeHandler) {
        escapeHandler = (e) => {
            if (e.key === 'Escape') {
                const top = getTopDrawer();
                if (top && top.closeOnEscape && !top.isDisposed && top.dotnet) {
                    e.preventDefault();
                    e.stopPropagation();
                    try { top.dotnet.invokeMethodAsync('RequestCloseAsync').catch(() => {}); } catch {}
                }
            }
        };
        document.addEventListener('keydown', escapeHandler, true);
    }

    if (!focusTrapHandler) {
        focusTrapHandler = (e) => {
            if (e.key !== 'Tab') return;
            const top = getTopDrawer();
            if (!top || top.isDisposed) return;
            const focusable = getFocusableElements(top.element);
            if (focusable.length === 0) {
                e.preventDefault();
                top.element.focus();
                return;
            }
            const first = focusable[0];
            const last = focusable[focusable.length - 1];
            const active = document.activeElement;
            if (e.shiftKey) {
                if (active === first || !top.element.contains(active)) {
                    e.preventDefault();
                    last.focus();
                }
            } else {
                if (active === last || !top.element.contains(active)) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };
        document.addEventListener('keydown', focusTrapHandler, true);
    }

    setTimeout(() => {
        if (!entry.isDisposed && entry.element && entry.element.isConnected) {
            const focusable = getFocusableElements(entry.element);
            if (focusable.length > 0) focusable[0].focus();
            else entry.element.focus();
        }
    }, 50);
}

export function initResize(drawerElement, dotnetRef, placement, minSize, maxSize) {
    const resizer = drawerElement.querySelector('.sgc-drawer-resizer');
    if (!resizer) return;

    const minPx = parseInt(minSize, 10) || 200;
    const maxPx = maxSize ? parseInt(maxSize, 10) : Infinity;
    let isResizing = false;
    let startSize = 0;
    let startPos = 0;
    let isDisposed = false;

    const onMove = (e) => {
        if (!isResizing || isDisposed) return;
        let delta = 0;
        if (placement === 'sgc-drawer-right') delta = startPos - e.clientX;
        else if (placement === 'sgc-drawer-left') delta = e.clientX - startPos;
        else if (placement === 'sgc-drawer-bottom') delta = startPos - e.clientY;
        else if (placement === 'sgc-drawer-top') delta = e.clientY - startPos;

        const newSize = Math.max(minPx, Math.min(maxPx, startSize + delta));
        if (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right') {
            drawerElement.style.width = newSize + 'px';
        } else {
            drawerElement.style.height = newSize + 'px';
        }
    };

    const onUp = () => {
        if (!isResizing) return;
        isResizing = false;
        document.removeEventListener('pointermove', onMove);
        document.removeEventListener('pointerup', onUp);
        document.removeEventListener('pointercancel', onUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        if (!isDisposed && dotnetRef) {
            const currentSize = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right')
                ? drawerElement.offsetWidth : drawerElement.offsetHeight;
            try { dotnetRef.invokeMethodAsync('UpdateSizeFromJs', currentSize + 'px').catch(() => {}); } catch {}
        }
    };

    resizer.addEventListener('pointerdown', (e) => {
        if (isDisposed) return;
        e.preventDefault();
        isResizing = true;
        startSize = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right')
            ? drawerElement.offsetWidth : drawerElement.offsetHeight;
        startPos = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right') ? e.clientX : e.clientY;
        document.addEventListener('pointermove', onMove);
        document.addEventListener('pointerup', onUp);
        document.addEventListener('pointercancel', onUp);
        document.body.style.cursor = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right') ? 'col-resize' : 'row-resize';
        document.body.style.userSelect = 'none';
    });

    resizer._dispose = () => { isDisposed = true; dotnetRef = null; };
}

export function detach(drawerElement) {
    const index = drawerStack.findIndex(x => x.element === drawerElement);
    if (index === -1) return;
    const entry = drawerStack.splice(index, 1)[0];
    if (entry.dispose) entry.dispose();

    if (index === drawerStack.length && entry.previousFocus && typeof entry.previousFocus.focus === 'function') {
        try { entry.previousFocus.focus(); } catch {}
    }

    const resizer = drawerElement.querySelector('.sgc-drawer-resizer');
    if (resizer && resizer._dispose) { resizer._dispose(); delete resizer._dispose; }

    if (drawerStack.length === 0) {
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        if (previousScrollPosition > 0) window.scrollTo(0, previousScrollPosition);
        if (escapeHandler) { document.removeEventListener('keydown', escapeHandler, true); escapeHandler = null; }
        if (focusTrapHandler) { document.removeEventListener('keydown', focusTrapHandler, true); focusTrapHandler = null; }
    }
}
