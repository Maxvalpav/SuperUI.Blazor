// superui-drawer.js - Drawer focus-trap, ESC handling, return-focus, scroll-lock, and resize support

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

export function attach(drawerElement, dotnetRef, closeOnEscape) {
    const previousFocus = document.activeElement;

    // Lock body scroll if it's the first drawer
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

    // Global ESC key handler
    if (!escapeHandler) {
        escapeHandler = (e) => {
            if (e.key === 'Escape') {
                const top = getTopDrawer();
                if (top && top.closeOnEscape && !top.isDisposed && top.dotnet) {
                    e.preventDefault();
                    e.stopPropagation();
                    try {
                        top.dotnet.invokeMethodAsync('CloseFromJsAsync').catch(() => {});
                    } catch { }
                }
            }
        };
        document.addEventListener('keydown', escapeHandler, true);
    }

    // Global focus-trap handler
    if (!focusTrapHandler) {
        focusTrapHandler = (e) => {
            if (e.key !== 'Tab') return;
            const top = getTopDrawer();
            if (!top || top.isDisposed) return;

            const focusableElements = getFocusableElements(top.element);
            if (focusableElements.length === 0) {
                // If no focusable elements, focus the drawer itself
                e.preventDefault();
                top.element.focus();
                return;
            }

            const firstFocusable = focusableElements[0];
            const lastFocusable = focusableElements[focusableElements.length - 1];
            const activeElement = document.activeElement;

            if (e.shiftKey) {
                if (activeElement === firstFocusable || !top.element.contains(activeElement)) {
                    e.preventDefault();
                    lastFocusable.focus();
                }
            } else {
                if (activeElement === lastFocusable || !top.element.contains(activeElement)) {
                    e.preventDefault();
                    firstFocusable.focus();
                }
            }
        };
        document.addEventListener('keydown', focusTrapHandler, true);
    }

    // Focus first focusable element in drawer
    setTimeout(() => {
        if (!entry.isDisposed && entry.element && entry.element.isConnected) {
            const focusableElements = getFocusableElements(entry.element);
            if (focusableElements.length > 0) {
                focusableElements[0].focus();
            } else {
                entry.element.focus();
            }
        }
    }, 50);
}

export function initResize(drawerElement, dotnetRef, placement) {
    const resizer = drawerElement.querySelector('.sgc-drawer-resizer');
    if (!resizer) return;

    let isResizing = false;
    let startSize = 0;
    let startPos = 0;
    let isDisposed = false;

    const onPointerMove = (e) => {
        if (!isResizing || isDisposed) return;

        let delta = 0;
        if (placement === 'right') {
            delta = startPos - e.clientX;
        } else if (placement === 'left') {
            delta = e.clientX - startPos;
        } else if (placement === 'bottom') {
            delta = startPos - e.clientY;
        } else if (placement === 'top') {
            delta = e.clientY - startPos;
        }

        const newSize = Math.max(100, startSize + delta);
        const sizeStr = `${newSize}px`;
        
        if (placement === 'left' || placement === 'right') {
            drawerElement.style.width = sizeStr;
        } else {
            drawerElement.style.height = sizeStr;
        }
    };

    const onPointerUp = () => {
        if (!isResizing) return;
        isResizing = false;
        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.removeEventListener('pointercancel', onPointerUp);
        document.body.style.cursor = '';
        
        if (!isDisposed && dotnetRef) {
            const currentSize = (placement === 'left' || placement === 'right') 
                ? drawerElement.offsetWidth 
                : drawerElement.offsetHeight;
            try {
                dotnetRef.invokeMethodAsync('UpdateSizeFromJs', `${currentSize}px`).catch(() => {});
            } catch { }
        }
    };

    resizer.style.cursor = (placement === 'left' || placement === 'right') ? 'col-resize' : 'row-resize';
    
    resizer.addEventListener('pointerdown', (e) => {
        if (isDisposed) return;
        e.preventDefault();
        isResizing = true;
        startSize = (placement === 'left' || placement === 'right') 
            ? drawerElement.offsetWidth 
            : drawerElement.offsetHeight;
        startPos = (placement === 'left' || placement === 'right') ? e.clientX : e.clientY;

        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup', onPointerUp);
        document.addEventListener('pointercancel', onPointerUp);
        document.body.style.cursor = (placement === 'left' || placement === 'right') ? 'col-resize' : 'row-resize';
    });

    // Store dispose function on resizer for cleanup
    resizer._dispose = () => {
        isDisposed = true;
        dotnetRef = null;
    };
}

export function detach() {
    const entry = drawerStack.pop();
    if (!entry) return;

    // Mark as disposed first
    if (entry.dispose) entry.dispose();

    // Restore previous focus
    if (entry.previousFocus && typeof entry.previousFocus.focus === 'function') {
        try {
            entry.previousFocus.focus();
        } catch (e) {
            // Element might have been removed from DOM
        }
    }

    // If no more drawers, cleanup global handlers and unlock scroll
    if (drawerStack.length === 0) {
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        
        // Restore scroll position
        if (previousScrollPosition > 0) {
            window.scrollTo(0, previousScrollPosition);
        }
        
        if (escapeHandler) {
            document.removeEventListener('keydown', escapeHandler, true);
            escapeHandler = null;
        }
        if (focusTrapHandler) {
            document.removeEventListener('keydown', focusTrapHandler, true);
            focusTrapHandler = null;
        }
    }
}

function getScrollbarWidth() {
    const outer = document.createElement('div');
    outer.style.visibility = 'hidden';
    outer.style.overflow = 'scroll';
    document.body.appendChild(outer);
    
    const inner = document.createElement('div');
    outer.appendChild(inner);
    
    const scrollbarWidth = outer.offsetWidth - inner.offsetWidth;
    outer.parentNode.removeChild(outer);
    
    return scrollbarWidth;
}
