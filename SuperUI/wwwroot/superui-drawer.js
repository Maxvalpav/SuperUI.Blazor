// superui-drawer.js — Focus-trap, ESC, scroll-lock, resize, swipe, cascade

const drawerStack = [];
let escapeHandler = null;
let focusTrapHandler = null;
let previousScrollPosition = 0;
let touchStartX = 0;
let touchStartY = 0;

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

function getPlacement(drawerElement) {
    if (drawerElement.classList.contains('sgc-drawer-left')) return 'left';
    if (drawerElement.classList.contains('sgc-drawer-top')) return 'top';
    if (drawerElement.classList.contains('sgc-drawer-bottom')) return 'bottom';
    return 'right';
}

function updateCascadeOffset() {
    for (let i = 0; i < drawerStack.length; i++) {
        const entry = drawerStack[i];
        if (!entry || entry.isDisposed || !entry.element || !entry.element.isConnected) continue;
        const offset = i * 16;
        const el = entry.element;
        const placement = getPlacement(el);
        if (placement === 'right') el.style.transform = `translateX(-${offset}px)`;
        else if (placement === 'left') el.style.transform = `translateX(${offset}px)`;
        else if (placement === 'top') el.style.transform = `translateY(${offset}px)`;
        else if (placement === 'bottom') el.style.transform = `translateY(-${offset}px)`;
    }
}

export function attach(drawerElement, dotnetRef, closeOnEscape, autoFocus, disableScrollLock) {
    const previousFocus = document.activeElement;
    const placement = getPlacement(drawerElement);

    if (!disableScrollLock && drawerStack.length === 0) {
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
        placement,
        dispose: () => {
            entry.isDisposed = true;
            entry.dotnet = null;
        }
    };

    drawerStack.push(entry);
    updateCascadeOffset();

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
                if (active === first || !top.element.contains(active)) { e.preventDefault(); last.focus(); }
            } else {
                if (active === last || !top.element.contains(active)) { e.preventDefault(); first.focus(); }
            }
        };
        document.addEventListener('keydown', focusTrapHandler, true);
    }

    // Autofocus
    if (autoFocus !== false) {
        setTimeout(() => {
            if (!entry.isDisposed && entry.element && entry.element.isConnected) {
                const focusable = getFocusableElements(entry.element);
                if (focusable.length > 0) focusable[0].focus();
                else entry.element.focus();
            }
        }, 80);
    }

    // Swipe-to-close
    const onTouchStart = (e) => {
        touchStartX = e.touches[0].clientX;
        touchStartY = e.touches[0].clientY;
    };
    const onTouchEnd = (e) => {
        if (entry.isDisposed) return;
        const dx = e.changedTouches[0].clientX - touchStartX;
        const dy = e.changedTouches[0].clientY - touchStartY;
        const absDx = Math.abs(dx);
        const absDy = Math.abs(dy);
        const threshold = 80;

        let swipeDetected = false;
        if (placement === 'right' && dx < -threshold && absDx > absDy * 1.5) swipeDetected = true;
        else if (placement === 'left' && dx > threshold && absDx > absDy * 1.5) swipeDetected = true;
        else if (placement === 'top' && dy > threshold && absDy > absDx * 1.5) swipeDetected = true;
        else if (placement === 'bottom' && dy < -threshold && absDy > absDx * 1.5) swipeDetected = true;

        if (swipeDetected && entry.dotnet) {
            try { entry.dotnet.invokeMethodAsync('OnSwipeClose').catch(() => {}); } catch {}
        }
    };
    drawerElement.addEventListener('touchstart', onTouchStart, { passive: true });
    drawerElement.addEventListener('touchend', onTouchEnd, { passive: true });
    entry._swipeCleanup = () => {
        drawerElement.removeEventListener('touchstart', onTouchStart);
        drawerElement.removeEventListener('touchend', onTouchEnd);
    };
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

    const onDown = (e) => {
        if (isDisposed) return;
        e.preventDefault();
        isResizing = true;
        startSize = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right')
            ? drawerElement.offsetWidth : drawerElement.offsetHeight;
        startPos = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right') ? e.clientX : e.clientY;
        document.addEventListener('pointermove', onMove);
        document.addEventListener('pointerup', onUp);
        document.addEventListener('pointercancel', onUp);
        const cursor = (placement === 'sgc-drawer-left' || placement === 'sgc-drawer-right') ? 'col-resize' : 'row-resize';
        document.body.style.cursor = cursor;
        document.body.style.userSelect = 'none';
    };

    resizer.addEventListener('pointerdown', onDown);
    resizer._dispose = () => {
        isDisposed = true;
        dotnetRef = null;
        resizer.removeEventListener('pointerdown', onDown);
        document.removeEventListener('pointermove', onMove);
        document.removeEventListener('pointerup', onUp);
        document.removeEventListener('pointercancel', onUp);
    };
}

export function detach(drawerElement) {
    const index = drawerStack.findIndex(x => x.element === drawerElement);
    if (index === -1) return;
    const entry = drawerStack.splice(index, 1)[0];
    if (entry.dispose) entry.dispose();
    if (entry._swipeCleanup) entry._swipeCleanup();

    if (index === drawerStack.length && entry.previousFocus && typeof entry.previousFocus.focus === 'function') {
        try { entry.previousFocus.focus(); } catch {}
    }

    const resizer = drawerElement.querySelector('.sgc-drawer-resizer');
    if (resizer && resizer._dispose) { resizer._dispose(); delete resizer._dispose; }

    updateCascadeOffset();

    if (drawerStack.length === 0) {
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        if (previousScrollPosition > 0) window.scrollTo(0, previousScrollPosition);
        if (escapeHandler) { document.removeEventListener('keydown', escapeHandler, true); escapeHandler = null; }
        if (focusTrapHandler) { document.removeEventListener('keydown', focusTrapHandler, true); focusTrapHandler = null; }
    }
}
