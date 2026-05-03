// superui-popover.js - Popover smart positioning, focus management, ESC handling, and cleanup

const handlers = new WeakMap();

function getFocusableElements(element) {
    return Array.from(element.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
    ));
}

function getSmartPosition(triggerElement, popoverElement, placement) {
    const triggerRect = triggerElement.getBoundingClientRect();
    const popoverRect = popoverElement.getBoundingClientRect();
    const padding = 8;
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    // Calculate initial position based on placement
    let top = 0, left = 0;
    let finalPlacement = placement;

    switch (placement) {
        case 'top':
        case 'top-start':
        case 'top-end':
            top = triggerRect.top - popoverRect.height - padding;
            if (top < 0) {
                // Not enough space on top, move to bottom
                top = triggerRect.bottom + padding;
                finalPlacement = placement.replace('top', 'bottom');
            }
            break;
        case 'bottom':
        case 'bottom-start':
        case 'bottom-end':
            top = triggerRect.bottom + padding;
            if (top + popoverRect.height > viewportHeight) {
                // Not enough space on bottom, move to top
                top = triggerRect.top - popoverRect.height - padding;
                finalPlacement = placement.replace('bottom', 'top');
            }
            break;
        case 'left':
        case 'left-start':
        case 'left-end':
            left = triggerRect.left - popoverRect.width - padding;
            if (left < 0) {
                // Not enough space on left, move to right
                left = triggerRect.right + padding;
                finalPlacement = placement.replace('left', 'right');
            }
            break;
        case 'right':
        case 'right-start':
        case 'right-end':
            left = triggerRect.right + padding;
            if (left + popoverRect.width > viewportWidth) {
                // Not enough space on right, move to left
                left = triggerRect.left - popoverRect.width - padding;
                finalPlacement = placement.replace('right', 'left');
            }
            break;
    }

    // Adjust horizontal position for start/end variants
    if (finalPlacement.includes('start')) {
        left = triggerRect.left;
    } else if (finalPlacement.includes('end')) {
        left = triggerRect.right - popoverRect.width;
    } else if (finalPlacement.includes('top') || finalPlacement.includes('bottom')) {
        left = triggerRect.left + (triggerRect.width - popoverRect.width) / 2;
    }

    // Adjust vertical position for start/end variants
    if (finalPlacement.includes('start')) {
        top = triggerRect.top;
    } else if (finalPlacement.includes('end')) {
        top = triggerRect.bottom - popoverRect.height;
    } else if (finalPlacement.includes('left') || finalPlacement.includes('right')) {
        top = triggerRect.top + (triggerRect.height - popoverRect.height) / 2;
    }

    // Keep within viewport bounds
    if (left < padding) {
        left = padding;
    } else if (left + popoverRect.width > viewportWidth - padding) {
        left = viewportWidth - popoverRect.width - padding;
    }

    if (top < padding) {
        top = padding;
    } else if (top + popoverRect.height > viewportHeight - padding) {
        top = viewportHeight - popoverRect.height - padding;
    }

    return { top, left };
}

export function attach(root, popoverElement, triggerElement, dotnetRef, closeOnOutsideClick, closeOnEscape) {
    // Clean up any existing handlers first
    detach(root);

    let isDisposed = false;

    const onPointerDown = (event) => {
        if (isDisposed || !closeOnOutsideClick) return;
        if (!root || root.contains(event.target)) return;

        try {
            if (dotnetRef && !isDisposed) {
                dotnetRef.invokeMethodAsync("CloseFromJsAsync").catch(() => {});
            }
        } catch { }
    };

    const onKeyDown = (event) => {
        if (isDisposed || !closeOnEscape || event.key !== "Escape") return;

        try {
            if (dotnetRef && !isDisposed) {
                dotnetRef.invokeMethodAsync("CloseFromJsAsync").catch(() => {});
            }
        } catch { }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    
    // Store handlers for cleanup
    handlers.set(root, { 
        onPointerDown, 
        onKeyDown, 
        triggerElement,
        isDisposed: false,
        dispose: () => {
            isDisposed = true;
            dotnetRef = null;
        }
    });

    // Smart positioning and focus management
    setTimeout(() => {
        if (isDisposed || !popoverElement) return;

        // Get current placement from CSS classes
        let placement = 'bottom-start';
        const classList = Array.from(popoverElement.classList);
        const placementClass = classList.find(c => c.startsWith('sgc-pop-'));
        if (placementClass) {
            placement = placementClass.replace('sgc-pop-', '');
        }

        // Calculate smart position
        const pos = getSmartPosition(triggerElement, popoverElement, placement);
        
        // Apply position adjustments
        popoverElement.style.position = 'fixed';
        popoverElement.style.top = pos.top + 'px';
        popoverElement.style.left = pos.left + 'px';

        // Focus first focusable element in popover
        const focusableElements = getFocusableElements(popoverElement);
        if (focusableElements.length > 0) {
            focusableElements[0].focus();
        } else {
            popoverElement.focus();
        }
    }, 50);
}

export function detach(root) {
    const entry = handlers.get(root);
    if (!entry) return;

    // Mark as disposed first
    if (entry.dispose) entry.dispose();

    document.removeEventListener("pointerdown", entry.onPointerDown);
    document.removeEventListener("keydown", entry.onKeyDown);
    
    // Restore focus to trigger element
    if (entry.triggerElement && typeof entry.triggerElement.focus === 'function') {
        try {
            entry.triggerElement.focus();
        } catch (e) { }
    }

    handlers.delete(root);
}
