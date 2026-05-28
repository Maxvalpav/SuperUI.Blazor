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

    let maxHeight = viewportHeight - padding * 2;
    if (top < padding) {
        top = padding;
    } else if (top + popoverRect.height > viewportHeight - padding) {
        // If it still doesn't fit after flipping, we MUST limit height
        maxHeight = viewportHeight - top - padding;
    }

    return { top, left, placement: finalPlacement, maxHeight };
}

export function attach(root, popoverElement, triggerElement, dotnetRef, closeOnOutsideClick, closeOnEscape) {
    // Clean up any existing handlers first
    detach(root);

    // Hide until JS positions it to avoid flash at wrong coordinates
    popoverElement.style.visibility = 'hidden';

    let isDisposed = false;

    const onPointerDown = (event) => {
        if (isDisposed || !closeOnOutsideClick || !dotnetRef) return;
        
        // Check if click is inside the wrapper OR the popover (which is teleported)
        const isInsideRoot = root && root.contains(event.target);
        const isInsidePop = popoverElement && popoverElement.contains(event.target);
        
        if (isInsideRoot || isInsidePop) return;

        try {
            // Check if dotnetRef is still valid (Blazor specific check)
            if (typeof dotnetRef.invokeMethodAsync === 'function') {
                dotnetRef.invokeMethodAsync("CloseFromJsAsync").catch(() => {});
            }
        } catch (err) {
            console.warn("SgPopover: failed to invoke CloseFromJsAsync", err);
        }
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

        // Get current placement from CSS classes. Only sgc-pop-<placement> classes are
        // considered — generic helpers like sgc-pop-arrow / sgc-pop-wrap / sgc-pop-body
        // must not be picked up.
        const validPlacements = [
            'top', 'top-start', 'top-end',
            'bottom', 'bottom-start', 'bottom-end',
            'left', 'left-start', 'left-end',
            'right', 'right-start', 'right-end'
        ];
        let placement = 'bottom-start';
        let originalClass = null;
        for (const cls of popoverElement.classList) {
            if (cls.startsWith('sgc-pop-')) {
                const candidate = cls.substring('sgc-pop-'.length);
                if (validPlacements.includes(candidate)) {
                    placement = candidate;
                    originalClass = cls;
                    break;
                }
            }
        }

        // Calculate smart position (may flip placement to fit viewport)
        const pos = getSmartPosition(triggerElement, popoverElement, placement);

        // If JS flipped the side, swap the CSS class so the arrow + variant styles match.
        if (originalClass && pos.placement !== placement) {
            popoverElement.classList.remove(originalClass);
            popoverElement.classList.add('sgc-pop-' + pos.placement);
        }

        // Apply position adjustments. Reset any CSS-class anchors that would conflict
        // with the fixed-position coords we just computed.
        popoverElement.style.position = 'fixed';
        popoverElement.style.top = pos.top + 'px';
        popoverElement.style.left = pos.left + 'px';
        popoverElement.style.maxHeight = pos.maxHeight + 'px';
        popoverElement.style.display = 'flex'; // Ensure flex for height limit
        popoverElement.style.flexDirection = 'column';
        popoverElement.style.right = 'auto';
        popoverElement.style.bottom = 'auto';
        popoverElement.style.transform = 'none';
        popoverElement.style.margin = '0';

        // Make visible only after correct positioning to avoid flash
        popoverElement.style.visibility = 'visible';

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
