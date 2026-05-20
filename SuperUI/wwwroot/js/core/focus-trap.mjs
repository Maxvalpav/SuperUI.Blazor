/**
 * SuperUI Focus Trap
 * Ensures focus remains within a container while active.
 */

const focusableSelectors = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

export function createFocusTrap(element) {
    if (!element) return null;

    const onKeyDown = (e) => {
        if (e.key !== 'Tab') return;

        const focusables = element.querySelectorAll(focusableSelectors);
        if (focusables.length === 0) return;

        const first = focusables[0];
        const last = focusables[focusables.length - 1];

        if (e.shiftKey) {
            if (document.activeElement === first) {
                last.focus();
                e.preventDefault();
            }
        } else {
            if (document.activeElement === last) {
                first.focus();
                e.preventDefault();
            }
        }
    };

    element.addEventListener('keydown', onKeyDown);
    
    // Auto-focus first element
    const first = element.querySelector(focusableSelectors);
    if (first) first.focus();

    return {
        dispose: () => {
            element.removeEventListener('keydown', onKeyDown);
        }
    };
}
