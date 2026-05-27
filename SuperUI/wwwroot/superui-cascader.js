const handlers = new WeakMap();

export function attach(root, dotnetRef) {
    detach(root);

    let isDisposed = false;

    const onPointerDown = (event) => {
        if (isDisposed) return;
        if (!root || root.contains(event.target)) return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    const onKeyDown = (event) => {
        if (isDisposed || event.key !== "Escape") return;
        try {
            dotnetRef?.invokeMethodAsync("CloseFromJsAsync")?.catch(() => {});
        } catch { }
    };

    const onScroll = () => {
        if (isDisposed) return;
        const menu = root.querySelector('.sgc-cascader-menu');
        if (menu) {
            const rect = root.getBoundingClientRect();
            menu.style.position = 'fixed';
            menu.style.top = (rect.bottom + 4) + 'px';
            menu.style.left = rect.left + 'px';
            menu.style.minWidth = rect.width + 'px';
        }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("scroll", onScroll, true);
    window.addEventListener("resize", onScroll);

    handlers.set(root, {
        onPointerDown,
        onKeyDown,
        onScroll,
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
    window.removeEventListener("scroll", entry.onScroll, true);
    window.removeEventListener("resize", entry.onScroll);
    handlers.delete(root);
}
