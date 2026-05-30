// superui-popover.js — Smart positioning for popovers with portal support
// Supports: offset, scroll/resize repositioning, auto-flip, interactive mode

const popoverInstances = new WeakMap();

function getFocusableElements(element) {
    return Array.from(element.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
    ));
}

function findScrollParents(el) {
    const parents = [];
    let parent = el.parentElement;
    while (parent && parent !== document.body) {
        const style = getComputedStyle(parent);
        if (style.overflow === 'auto' || style.overflow === 'scroll' ||
            style.overflowY === 'auto' || style.overflowY === 'scroll' ||
            style.overflowX === 'auto' || style.overflowX === 'scroll') {
            parents.push(parent);
        }
        parent = parent.parentElement;
    }
    parents.push(window);
    return parents;
}

function positionPopover(trigger, popover, placement, offset, closeOnEscape, interactive) {
    if (!trigger || !popover) return;

    const triggerRect = trigger.getBoundingClientRect();
    const popoverRect = popover.getBoundingClientRect();
    const padding = 8;
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    const parts = placement.split('-');
    const dir = parts[0];
    const align = parts[1] || 'center';

    let top, left;

    if (dir === 'bottom') {
        top = triggerRect.bottom + offset;
        if (align === 'start') left = triggerRect.left;
        else if (align === 'end') left = triggerRect.right - popoverRect.width;
        else left = triggerRect.left + (triggerRect.width - popoverRect.width) / 2;
    } else if (dir === 'top') {
        top = triggerRect.top - popoverRect.height - offset;
        if (align === 'start') left = triggerRect.left;
        else if (align === 'end') left = triggerRect.right - popoverRect.width;
        else left = triggerRect.left + (triggerRect.width - popoverRect.width) / 2;
    } else if (dir === 'left') {
        left = triggerRect.left - popoverRect.width - offset;
        if (align === 'start') top = triggerRect.top;
        else if (align === 'end') top = triggerRect.bottom - popoverRect.height;
        else top = triggerRect.top + (triggerRect.height - popoverRect.height) / 2;
    } else if (dir === 'right') {
        left = triggerRect.right + offset;
        if (align === 'start') top = triggerRect.top;
        else if (align === 'end') top = triggerRect.bottom - popoverRect.height;
        else top = triggerRect.top + (triggerRect.height - popoverRect.height) / 2;
    }

    // Auto-flip if out of viewport
    if (dir === 'top' && top < padding) {
        top = triggerRect.bottom + offset;
    } else if (dir === 'bottom' && top + popoverRect.height > vh - padding) {
        top = triggerRect.top - popoverRect.height - offset;
    } else if (dir === 'left' && left < padding) {
        left = triggerRect.right + offset;
    } else if (dir === 'right' && left + popoverRect.width > vw - padding) {
        left = triggerRect.left - popoverRect.width - offset;
    }

    // Clamp within viewport
    left = Math.max(padding, Math.min(left, vw - popoverRect.width - padding));
    top = Math.max(padding, Math.min(top, vh - popoverRect.height - padding));

    // Limit height to avoid overflow
    let maxHeight = vh - padding * 2;
    if (top < padding) {
        top = padding;
    } else if (top + popoverRect.height > vh - padding) {
        maxHeight = vh - top - padding;
    }

    popover.style.position = 'fixed';
    popover.style.top = `${top}px`;
    popover.style.left = `${left}px`;
    popover.style.maxHeight = `${Math.max(100, maxHeight)}px`;
    popover.style.right = 'auto';
    popover.style.bottom = 'auto';
    popover.style.transform = 'none';
    popover.style.margin = '0';
}

export function attach(root, popover, trigger, dotnetRef, closeOnOutsideClick, closeOnEscape, offset = 6, interactive = false) {
    detach(root);
    if (!root || !popover || !trigger) {
        console.warn('SuperUI Popover: attach failed - missing elements', { root, popover, trigger });
        return;
    }

    if (popover.style) {
        popover.style.visibility = 'hidden';
        popover.style.opacity = '0';
    }

    let isDisposed = false;
    let scrollParents = [];

    // Detect current placement from CSS class
    const validPlacements = [
        'top', 'top-start', 'top-end',
        'bottom', 'bottom-start', 'bottom-end',
        'left', 'left-start', 'left-end',
        'right', 'right-start', 'right-end'
    ];
    let placement = 'bottom-start';
    let originalClass = null;
    for (const cls of popover.classList) {
        if (cls.startsWith('sgc-pop-')) {
            const candidate = cls.substring('sgc-pop-'.length);
            if (validPlacements.includes(candidate)) {
                placement = candidate;
                originalClass = cls;
                break;
            }
        }
    }

    const onPointerDown = (event) => {
        if (isDisposed || !closeOnOutsideClick || !dotnetRef) return;
        const isInsideRoot = root && root.contains(event.target);
        const isInsidePop = popover && popover.contains(event.target);
        if (isInsideRoot || isInsidePop) return;
        try {
            if (typeof dotnetRef.invokeMethodAsync === 'function') {
                dotnetRef.invokeMethodAsync("CloseFromJsAsync").catch(() => {});
            }
        } catch {}
    };

    const onKeyDown = (event) => {
        if (isDisposed || !closeOnEscape || event.key !== "Escape") return;
        try {
            if (dotnetRef && !isDisposed) {
                dotnetRef.invokeMethodAsync("CloseFromJsAsync").catch(() => {});
            }
        } catch {}
    };

    const reposition = () => {
        if (isDisposed) return;
        positionPopover(trigger, popover, placement, offset, closeOnEscape, interactive);
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    scrollParents = findScrollParents(trigger);
    scrollParents.forEach(p => p.addEventListener('scroll', reposition, { passive: true }));

    const resizeObserver = new ResizeObserver(() => reposition());
    resizeObserver.observe(document.body);

    requestAnimationFrame(() => {
        if (isDisposed || !popover) return;

        positionPopover(trigger, popover, placement, offset, closeOnEscape, interactive);

        popover.style.visibility = 'visible';
        popover.style.opacity = '';

        const focusable = getFocusableElements(popover);
        if (focusable.length > 0) {
            focusable[0].focus();
        } else {
            popover.focus();
        }
    });

    const instance = {
        root, popover, trigger, dotnetRef,
        offset, placement, interactive,
        scrollParents, resizeObserver, reposition,
        onPointerDown, onKeyDown,
        isDisposed: false,
        dispose: () => {
            isDisposed = true;
            dotnetRef = null;
            document.removeEventListener("pointerdown", onPointerDown);
            document.removeEventListener("keydown", onKeyDown);
            scrollParents.forEach(p => p.removeEventListener('scroll', reposition));
            resizeObserver.disconnect();
        }
    };

    popoverInstances.set(root, instance);
}

export function detach(root) {
    const instance = popoverInstances.get(root);
    if (instance) {
        instance.dispose();
        popoverInstances.delete(root);
    }
}
