// superui-contextmenu.js

const _state = new WeakMap(); // menuElement → { esc, kbd, scroll, prevFocus, isDisposed, dispose }

function getFocusableItems(el) {
    return Array.from(el.querySelectorAll(
        'button:not([disabled]), a:not([disabled]), [role="menuitem"]:not([aria-disabled="true"])'
    ));
}

export function attach(menuElement, dotnetRef) {
    // Idempotent: detach any previous handlers on this element first.
    detachElement(menuElement);

    let isDisposed = false;
    const prevFocus = document.activeElement;

    const esc = (e) => {
        if (isDisposed || !dotnetRef) return;
        if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            try {
                dotnetRef.invokeMethodAsync('CloseFromJsAsync').catch(() => {});
            } catch { }
        }
    };

    const kbd = (e) => {
        if (isDisposed || !dotnetRef) return;
        if (!menuElement.contains(document.activeElement)) return;
        const items = getFocusableItems(menuElement);
        if (!items.length) return;
        const idx = items.indexOf(document.activeElement);
        switch (e.key) {
            case 'ArrowDown': e.preventDefault(); e.stopPropagation(); items[(idx + 1) % items.length].focus(); break;
            case 'ArrowUp':   e.preventDefault(); e.stopPropagation(); items[(idx - 1 + items.length) % items.length].focus(); break;
            case 'Home':      e.preventDefault(); e.stopPropagation(); items[0].focus(); break;
            case 'End':       e.preventDefault(); e.stopPropagation(); items[items.length - 1].focus(); break;
            case 'Enter':     if (idx >= 0) { e.preventDefault(); e.stopPropagation(); items[idx].click(); } break;
            case 'Tab': {
                const first = items[0], last = items[items.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); e.stopPropagation(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); e.stopPropagation(); first.focus(); }
                break;
            }
        }
    };

    const scroll = () => {
        if (isDisposed || !dotnetRef) return;
        try {
            dotnetRef.invokeMethodAsync('CloseFromJsAsync').catch(() => {});
        } catch { }
    };

    document.addEventListener('keydown', esc, true);
    document.addEventListener('keydown', kbd, true);
    window.addEventListener('scroll', scroll, true);

    _state.set(menuElement, { 
        esc, 
        kbd, 
        scroll, 
        prevFocus,
        isDisposed: false,
        dispose: function() {
            isDisposed = true;
            dotnetRef = null;
        }
    });

    setTimeout(() => { const items = getFocusableItems(menuElement); items[0]?.focus(); }, 50);
}

function detachElement(menuElement) {
    const s = _state.get(menuElement);
    if (!s) return;
    
    if (s.dispose) {
        s.dispose();
    }
    
    document.removeEventListener('keydown', s.esc, true);
    document.removeEventListener('keydown', s.kbd, true);
    window.removeEventListener('scroll', s.scroll, true);
    try { s.prevFocus?.focus(); } catch (_) {}
    _state.delete(menuElement);
}

export function detach(menuElement) {
    detachElement(menuElement);
}
