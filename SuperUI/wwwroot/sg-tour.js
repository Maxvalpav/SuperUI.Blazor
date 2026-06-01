/**
 * sg-tour.js - SuperUI Tour Component Bridge
 * Handles target element positioning and window event monitoring.
 */

export function getTargetRect(selector) {
    if (!selector) return null;
    const element = document.querySelector(selector);
    if (!element) return null;

    const rect = element.getBoundingClientRect();
    return {
        top: rect.top + window.scrollY,
        left: rect.left + window.scrollX,
        width: rect.width,
        height: rect.height,
        bottom: rect.bottom + window.scrollY,
        right: rect.right + window.scrollX
    };
}

export function scrollToElement(selector) {
    const element = document.querySelector(selector);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

export function observeWindow(dotnetRef) {
    const handler = () => {
        try { dotnetRef?.invokeMethodAsync('OnWindowChanged')?.catch(() => {}); } catch {}
    };
    window.addEventListener('resize', handler);
    window.addEventListener('scroll', handler, { passive: true });
    
    return {
        dispose: () => {
            window.removeEventListener('resize', handler);
            window.removeEventListener('scroll', handler, { passive: true });
        }
    };
}
