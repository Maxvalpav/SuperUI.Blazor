// wwwroot/focustrap.js
const FOCUSABLE = 'a[href],button:not([disabled]),input:not([disabled]),[tabindex]:not([tabindex="-1"])';
const traps = new Map();

export function activate(containerId) {
    const el = document.getElementById(containerId);
    if (!el) return;
    const focusable = [...el.querySelectorAll(FOCUSABLE)];
    if (focusable.length === 0) return;
    focusable[0]?.focus();
    const handler = (e) => {
        if (e.key !== 'Tab') return;
        const first = focusable[0], last = focusable[focusable.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    };
    document.addEventListener('keydown', handler);
    traps.set(containerId, handler);
}

export function deactivate(containerId) {
    const handler = traps.get(containerId);
    if (handler) { document.removeEventListener('keydown', handler); traps.delete(containerId); }
}
