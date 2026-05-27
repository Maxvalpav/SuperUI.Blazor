const handlers = new WeakMap();

export function attach(root, triggerElement, menuElement, dotnetRef, closeOnOutsideClick, closeOnEscape, flip, usePortal) {
    detach(root);

    let isDisposed = false;

    // ── Portal: position menu at fixed coords ──
    if (usePortal && menuElement) {
        const rect = triggerElement.getBoundingClientRect();
        menuElement.style.position = 'fixed';
        menuElement.style.left = rect.left + 'px';
        menuElement.style.top = (rect.bottom + 4) + 'px';
        menuElement.style.minWidth = rect.width + 'px';
        if (flip) {
            const spaceBelow = window.innerHeight - rect.bottom;
            const spaceRight = window.innerWidth - rect.left;
            const menuH = menuElement.offsetHeight || 200;
            const menuW = menuElement.offsetWidth || 200;
            const flipX = spaceRight < menuW;
            const flipY = spaceBelow < menuH;
            if (flipX) menuElement.style.left = Math.max(4, rect.right - (menuElement.offsetWidth || menuW)) + 'px';
            if (flipY) menuElement.style.top = (rect.top - (menuElement.offsetHeight || menuH) - 4) + 'px';
            try { dotnetRef?.invokeMethodAsync("ApplyFlip", flipX, flipY)?.catch(() => {}); } catch {}
        }
    }

    // ── Flip detection (non-portal) ──
    const doFlip = () => {
        if (!flip || usePortal || !menuElement) return;
        const m = menuElement;
        const r = m.getBoundingClientRect();
        const flipX = r.right > window.innerWidth - 8;
        const flipY = r.bottom > window.innerHeight - 8;
        try { dotnetRef?.invokeMethodAsync("ApplyFlip", flipX, flipY)?.catch(() => {}); } catch {}
    };

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

    const onScroll = () => {
        if (isDisposed) return;
        if (usePortal && menuElement) {
            const rect = triggerElement.getBoundingClientRect();
            menuElement.style.left = rect.left + 'px';
            menuElement.style.top = (rect.bottom + 4) + 'px';
        }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("scroll", onScroll, true);

    // Run flip detection on next frame once layout is settled
    if (flip) requestAnimationFrame(doFlip);

    handlers.set(root, {
        onPointerDown,
        onKeyDown,
        onScroll,
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
    window.removeEventListener("scroll", entry.onScroll, true);

    if (entry.triggerElement && typeof entry.triggerElement.focus === "function") {
        try { entry.triggerElement.focus(); } catch { }
    }

    handlers.delete(root);
}
