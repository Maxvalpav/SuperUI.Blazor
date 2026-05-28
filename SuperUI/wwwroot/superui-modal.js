// superui-modal.js - v2: focus-trap, ESC, scroll-lock, drag, resize, keyboard shortcuts, matchMedia

const modalStack = [];
let escapeHandler = null;
let focusTrapHandler = null;
let shortcutHandler = null;
let previousScrollPosition = 0;
const responsiveWatchers = new Map();

function getTopModal() {
    return modalStack[modalStack.length - 1];
}

function getFocusableElements(element) {
    return Array.from(element.querySelectorAll(
        'button:not([disabled]):not([hidden]), [href], input:not([disabled]):not([hidden]), select:not([disabled]):not([hidden]), textarea:not([disabled]):not([hidden]), [tabindex]:not([tabindex="-1"]):not([disabled]):not([hidden])'
    ));
}

export function attach(modalElement, dotnetRef, closeOnEscape, fullScreen, autoFocus, trapFocus, scrollLock) {
    const previousFocus = document.activeElement;

    if (modalStack.length === 0 && scrollLock !== false) {
        previousScrollPosition = window.scrollY || document.documentElement.scrollTop;
        document.body.style.overflow = 'hidden';
        document.body.style.paddingRight = getScrollbarWidth() + 'px';
    }

    const entry = {
        element: modalElement,
        dotnet: dotnetRef,
        closeOnEscape,
        fullScreen: !!fullScreen,
        autoFocus: autoFocus !== false,
        trapFocus: trapFocus !== false,
        previousFocus,
        isDisposed: false,
        dispose: () => {
            entry.isDisposed = true;
            entry.dotnet = null;
        }
    };

    modalStack.push(entry);

    if (!escapeHandler) {
        escapeHandler = (e) => {
            if (e.key === 'Escape') {
                const top = getTopModal();
                if (top && top.closeOnEscape && !top.isDisposed && top.dotnet) {
                    e.preventDefault();
                    e.stopPropagation();
                    try {
                        top.dotnet.invokeMethodAsync('RequestCloseAsync').catch(() => { });
                    } catch { }
                }
            }
        };
        document.addEventListener('keydown', escapeHandler, true);
    }

    if (entry.trapFocus) {
        if (!focusTrapHandler) {
            focusTrapHandler = (e) => {
                if (e.key !== 'Tab') return;
                const top = getTopModal();
                if (!top || top.isDisposed || !top.trapFocus) return;

                const focusableElements = getFocusableElements(top.element);
                if (focusableElements.length === 0) {
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
    }

    if (entry.autoFocus) {
        setTimeout(() => {
            if (!entry.isDisposed && entry.element && entry.element.isConnected) {
                const focusableElements = getFocusableElements(entry.element);
                if (focusableElements.length > 0) {
                    focusableElements[0].focus();
                } else {
                    const body = entry.element.querySelector('.sgc-modal-body');
                    if (body) body.focus();
                    else entry.element.focus();
                }
            }
        }, 50);
    }
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
        modalElement.style.transform = 'none';
        modalElement.style.left = `${initialX + dx}px`;
        modalElement.style.top = `${Math.max(0, initialY + dy)}px`;
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
        if (e.target.closest('button, input, [role="button"], select, textarea')) return;
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

export function initResize(modalElement) {
    if (!modalElement) return;

    const handles = modalElement.querySelectorAll('.sgc-modal-resize-handle');
    let isResizing = false;
    let startX, startY, startW, startH, startL, startT, dir;

    const onPointerMove = (e) => {
        if (!isResizing) return;
        e.preventDefault();
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;

        if (dir.includes('e')) modalElement.style.width = `${Math.max(300, startW + dx)}px`;
        if (dir.includes('w')) {
            modalElement.style.width = `${Math.max(300, startW - dx)}px`;
            modalElement.style.left = `${startL + dx}px`;
        }
        if (dir.includes('s')) modalElement.style.height = `${Math.max(200, startH + dy)}px`;
        if (dir.includes('n')) {
            modalElement.style.height = `${Math.max(200, startH - dy)}px`;
            modalElement.style.top = `${startT + dy}px`;
        }
    };

    const onPointerUp = () => {
        isResizing = false;
        document.removeEventListener('pointermove', onPointerMove);
        document.removeEventListener('pointerup', onPointerUp);
        document.removeEventListener('pointercancel', onPointerUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
    };

    handles.forEach(handle => {
        handle.addEventListener('pointerdown', (e) => {
            e.preventDefault();
            e.stopPropagation();
            isResizing = true;
            dir = handle.dataset.dir;
            const rect = modalElement.getBoundingClientRect();
            startX = e.clientX;
            startY = e.clientY;
            startW = rect.width;
            startH = rect.height;
            startL = rect.left;
            startT = rect.top;
            document.body.style.cursor = getComputedStyle(handle).cursor;
            document.body.style.userSelect = 'none';
            document.addEventListener('pointermove', onPointerMove);
            document.addEventListener('pointerup', onPointerUp);
            document.addEventListener('pointercancel', onPointerUp);
        });
    });
}

export function initShortcuts(modalElement, dotnetRef, shortcutKey) {
    if (!shortcutKey) return;

    const handler = (e) => {
        const top = getTopModal();
        if (!top || top.element !== modalElement || top.isDisposed) return;

        const parts = shortcutKey.toLowerCase().split('+');
        const key = parts[parts.length - 1];
        const ctrl = parts.includes('ctrl');
        const shift = parts.includes('shift');
        const alt = parts.includes('alt');

        if (e.key.toLowerCase() === key && e.ctrlKey === ctrl && e.shiftKey === shift && e.altKey === alt) {
            e.preventDefault();
            try {
                dotnetRef.invokeMethodAsync('OnSubmitAsync').catch(() => { });
            } catch { }
        }
    };

    document.addEventListener('keydown', handler, true);
    if (!shortcutHandler) {
        shortcutHandler = [];
    }
    shortcutHandler.push({ modalElement, handler });
}

export function watchResponsive(modalElement, dotnetRef) {
    const mql = window.matchMedia('(max-width: 768px)');
    const listener = (e) => {
        try {
            dotnetRef.invokeMethodAsync('OnResponsiveChangeAsync', e.matches).catch(() => { });
        } catch { }
    };
    mql.addEventListener('change', listener);
    responsiveWatchers.set(modalElement, { mql, listener });
}

export function unwatchResponsive(modalElement) {
    const watcher = responsiveWatchers.get(modalElement);
    if (watcher) {
        watcher.mql.removeEventListener('change', watcher.listener);
        responsiveWatchers.delete(modalElement);
    }
}

export function detach(modalElement) {
    const index = modalStack.findIndex(x => x.element === modalElement);
    if (index === -1) return;

    const entry = modalStack.splice(index, 1)[0];
    if (entry.dispose) entry.dispose();

    if (index === modalStack.length && entry.previousFocus && typeof entry.previousFocus.focus === 'function') {
        try {
            entry.previousFocus.focus();
        } catch (e) { }
    }

    if (shortcutHandler) {
        shortcutHandler = shortcutHandler.filter(s => {
            if (s.modalElement === modalElement) {
                document.removeEventListener('keydown', s.handler, true);
                return false;
            }
            return true;
        });
        if (shortcutHandler.length === 0) shortcutHandler = null;
    }

    if (modalStack.length === 0) {
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';

        if (previousScrollPosition > 0) {
            window.scrollTo(0, previousScrollPosition);
            previousScrollPosition = 0;
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
