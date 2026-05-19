// superui-modal.js - Modal focus-trap, ESC handling, scroll-lock, and drag support

const modalStack = [];
let escapeHandler = null;
let focusTrapHandler = null;
let previousScrollPosition = 0;

function getTopModal() {
    return modalStack[modalStack.length - 1];
}

function getFocusableElements(element) {
    return Array.from(element.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
    ));
}

export function attach(modalElement, dotnetRef, closeOnEscape) {
    const previousFocus = document.activeElement;
    let isDisposed = false;

    // Lock body scroll if it's the first modal
    if (modalStack.length === 0) {
        previousScrollPosition = window.scrollY || document.documentElement.scrollTop;
        document.body.style.overflow = 'hidden';
        document.body.style.paddingRight = getScrollbarWidth() + 'px';
    }

    const entry = {
        element: modalElement,
        dotnet: dotnetRef,
        closeOnEscape,
        previousFocus,
        dragHandler: null,
        dragMoveHandler: null,
        dragEndHandler: null,
        isDisposed: false,
        dispose: () => {
            entry.isDisposed = true;
            entry.dotnet = null;
        }
    };

    modalStack.push(entry);

    // Global handlers if not already added
    if (!escapeHandler) {
        escapeHandler = (e) => {
            if (e.key === 'Escape') {
                const top = getTopModal();
                if (top && top.closeOnEscape && !top.isDisposed && top.dotnet) {
                    e.preventDefault();
                    e.stopPropagation();
                    try {
                        top.dotnet.invokeMethodAsync('RequestCloseAsync').catch(() => {});
                    } catch { }
                }
            }
        };
        document.addEventListener('keydown', escapeHandler, true);
    }

    if (!focusTrapHandler) {
        focusTrapHandler = (e) => {
            if (e.key !== 'Tab') return;
            const top = getTopModal();
            if (!top || top.isDisposed) return;

            const focusableElements = getFocusableElements(top.element);
            if (focusableElements.length === 0) {
                // If no focusable elements, focus the modal itself
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

    // Focus first focusable element in modal
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

export function initDrag(modalElement, headerElement) {
    if (!headerElement) return;

    let isDragging = false;
    let startX, startY;
    let initialX, initialY;

    const onPointerMove = (e) => {
        if (!isDragging) return;
        e.preventDefault();
        
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        
        // Remove centering transform and set absolute position
        modalElement.style.transform = 'none';
        modalElement.style.left = `${initialX + dx}px`;
        modalElement.style.top = `${initialY + dy}px`;
        modalElement.style.margin = '0';
    };

    const onPointerUp = () => {
        isDragging = false;
        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.removeEventListener('pointercancel', onPointerUp);
    };

    headerElement.style.cursor = 'move';
    headerElement.style.userSelect = 'none';
    
    headerElement.addEventListener('pointerdown', (e) => {
        // Don't drag if clicking on interactive elements
        if (e.target.closest('button, input, [role="button"]')) return;
        
        isDragging = true;
        startX = e.clientX;
        startY = e.clientY;
        
        const rect = modalElement.getBoundingClientRect();
        initialX = rect.left;
        initialY = rect.top;

        document.addEventListener('pointermove', onPointerMove);
        document.addEventListener('pointerup', onPointerUp);
        document.addEventListener('pointercancel', onPointerUp);
    });
}

export function detach(modalElement) {
    const index = modalStack.findIndex(x => x.element === modalElement);
    if (index === -1) return;

    const entry = modalStack.splice(index, 1)[0];

    // Mark as disposed first
    if (entry.dispose) entry.dispose();

    // Restore previous focus if this was the top modal
    if (index === modalStack.length && entry.previousFocus && typeof entry.previousFocus.focus === 'function') {
        try {
            entry.previousFocus.focus();
        } catch (e) { }
    }

    // If no more modals, cleanup global handlers and unlock scroll
    if (modalStack.length === 0) {
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

let _cachedScrollbarWidth = null;

function getScrollbarWidth() {
    if (_cachedScrollbarWidth !== null) return _cachedScrollbarWidth;
    
    const outer = document.createElement('div');
    outer.style.visibility = 'hidden';
    outer.style.overflow = 'scroll';
    outer.style.position = 'absolute';
    outer.style.top = '-9999px';
    document.body.appendChild(outer);
    
    const inner = document.createElement('div');
    outer.appendChild(inner);
    
    _cachedScrollbarWidth = outer.offsetWidth - inner.offsetWidth;
    outer.parentNode.removeChild(outer);
    
    return _cachedScrollbarWidth;
}
