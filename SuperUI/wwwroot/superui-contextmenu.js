// superui-contextmenu.js - Context menu ESC handling, keyboard navigation, focus-trap, and scroll handling

let escapeHandler = null;
let keyboardHandler = null;
let scrollHandler = null;
let currentMenu = null;
let previousFocus = null;

function getFocusableItems(menuElement) {
    return Array.from(menuElement.querySelectorAll('button:not([disabled]), a:not([disabled]), [role="menuitem"]:not([aria-disabled="true"])'));
}

export function attach(menuElement, dotnetRef) {
    currentMenu = menuElement;
    previousFocus = document.activeElement;

    // ESC key handler - only for this menu
    escapeHandler = (e) => {
        if (e.key === 'Escape' && currentMenu) {
            e.preventDefault();
            e.stopPropagation();
            dotnetRef.invokeMethodAsync('CloseFromJsAsync');
        }
    };
    document.addEventListener('keydown', escapeHandler, true);

    // Keyboard navigation and Focus trap
    keyboardHandler = (e) => {
        if (!currentMenu || !currentMenu.contains(document.activeElement)) return;

        const items = getFocusableItems(menuElement);
        if (items.length === 0) return;

        const currentIndex = items.indexOf(document.activeElement);

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            e.stopPropagation();
            const nextIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
            items[nextIndex]?.focus();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            e.stopPropagation();
            const prevIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
            items[prevIndex]?.focus();
        } else if (e.key === 'Home') {
            e.preventDefault();
            e.stopPropagation();
            items[0]?.focus();
        } else if (e.key === 'End') {
            e.preventDefault();
            e.stopPropagation();
            items[items.length - 1]?.focus();
        } else if (e.key === 'Enter' && currentIndex >= 0) {
            e.preventDefault();
            e.stopPropagation();
            items[currentIndex]?.click();
        } else if (e.key === 'Tab') {
            // Focus trap for TAB - keep focus within menu
            const first = items[0];
            const last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                e.stopPropagation();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                e.stopPropagation();
                first.focus();
            }
        }
    };
    document.addEventListener('keydown', keyboardHandler, true);

    // Close on scroll - use capture phase to catch scroll before other handlers
    scrollHandler = () => {
        if (currentMenu) {
            dotnetRef.invokeMethodAsync('CloseFromJsAsync');
        }
    };
    window.addEventListener('scroll', scrollHandler, true);

    // Focus first item after a short delay to ensure DOM is ready
    setTimeout(() => {
        const items = getFocusableItems(menuElement);
        if (items.length > 0) {
            items[0].focus();
        }
    }, 50);
}

export function detach() {
    if (escapeHandler) {
        document.removeEventListener('keydown', escapeHandler, true);
        escapeHandler = null;
    }
    if (keyboardHandler) {
        document.removeEventListener('keydown', keyboardHandler, true);
        keyboardHandler = null;
    }
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler, true);
        scrollHandler = null;
    }
    
    // Restore focus to previous element if it still exists
    if (previousFocus && typeof previousFocus.focus === 'function') {
        try {
            previousFocus.focus();
        } catch (e) { }
    }
    
    currentMenu = null;
    previousFocus = null;
}
