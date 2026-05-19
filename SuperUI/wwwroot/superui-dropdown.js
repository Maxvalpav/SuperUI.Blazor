const handlers = new WeakMap();

export function attach(root, triggerElement, dotnetRef, closeOnOutsideClick, closeOnEscape) {
    detach(root);

    let isDisposed = false;

    const onPointerDown = (event) => {
        if (isDisposed || !closeOnOutsideClick) return;
        if (!root || root.contains(event.target)) return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    const onKeyDown = (event) => {
        if (isDisposed || !closeOnEscape || event.key !== "Escape") return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    handlers.set(root, {
        onPointerDown,
        onKeyDown,
        triggerElement,
        dispose: () => {
            isDisposed = true;
            dotnetRef = null;
        }
    });
}

export function detach(root) {
    const entry = handlers.get(root);
    if (!entry) return;

    if (entry.dispose) entry.dispose();
    document.removeEventListener("pointerdown", entry.onPointerDown);
    document.removeEventListener("keydown", entry.onKeyDown);

    if (entry.triggerElement && typeof entry.triggerElement.focus === "function") {
        try { entry.triggerElement.focus(); } catch { }
    }

    handlers.delete(root);
}
