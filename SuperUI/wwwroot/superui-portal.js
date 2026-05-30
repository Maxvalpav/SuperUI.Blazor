// superui-portal.js — Teleport with stacking, scroll lock, focus trap, observers
const originalParents = new WeakMap();
const portalConfigs = new Map();
const portalDropdowns = new Map();

let zIndexCounter = 9000;
let scrollLockCount = 0;
let prevBodyOverflow = '';
let prevBodyPaddingRight = '';

function getTarget(selector) {
    if (!selector || selector === 'body') return document.body;
    if (selector.startsWith('#')) return document.getElementById(selector.slice(1));
    try { return document.querySelector(selector) ?? document.body; }
    catch { return document.body; }
}

function getFocusable(el) {
    if (!el) return [];
    const sel = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"]), [contenteditable]';
    return Array.from(el.querySelectorAll(sel)).filter(e => e.offsetParent !== null);
}

function lockScroll() {
    if (scrollLockCount === 0) {
        prevBodyOverflow = document.body.style.overflow;
        prevBodyPaddingRight = document.body.style.paddingRight;
        const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
        document.body.style.overflow = 'hidden';
        if (scrollbarWidth > 0) {
            document.body.style.paddingRight = scrollbarWidth + 'px';
        }
    }
    scrollLockCount++;
}

function unlockScroll() {
    scrollLockCount = Math.max(0, scrollLockCount - 1);
    if (scrollLockCount === 0) {
        document.body.style.overflow = prevBodyOverflow;
        document.body.style.paddingRight = prevBodyPaddingRight;
    }
}

let prevActiveElement = null;

// ── Open portal with full options ──
export function open(element, options) {
    if (!element) return;

    const cfg = {
        zIndex: options?.zIndex || 0,
        preventScroll: !!options?.preventScroll,
        autoFocus: !!options?.autoFocus,
        renderAt: options?.renderAt || null,
        transitionDuration: options?.transitionDuration || null,
    };

    // Save active element for focus restore
    if (document.activeElement && document.activeElement !== document.body) {
        prevActiveElement = document.activeElement;
    }

    // Store original parent once
    if (!originalParents.has(element)) {
        originalParents.set(element, element.parentNode);
    }

    // Resolve z-index
    const zIndex = cfg.zIndex > 0 ? cfg.zIndex : (++zIndexCounter);

    // Teleport to target
    const target = getTarget(cfg.renderAt);
    if (element.parentElement !== target || element !== target.lastElementChild) {
        target.appendChild(element);
    }

    element.style.zIndex = zIndex;
    element.style.visibility = 'visible';

    // Scroll lock
    if (cfg.preventScroll) lockScroll();

    // Transition duration
    if (cfg.transitionDuration > 0) {
        element.style.animationDuration = cfg.transitionDuration + 'ms';
    }

    // Focus trap
    let focusTrapHandler = null;
    if (cfg.autoFocus || cfg.restoreFocus) {
        focusTrapHandler = (e) => {
            if (e.key !== 'Tab') return;
            const focusable = getFocusable(element);
            if (focusable.length === 0) { e.preventDefault(); return; }
            const first = focusable[0];
            const last = focusable[focusable.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault(); last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault(); first.focus();
            }
        };
        element.addEventListener('keydown', focusTrapHandler);

        // Auto-focus first focusable
        if (cfg.autoFocus) {
            requestAnimationFrame(() => {
                const f = getFocusable(element);
                if (f.length > 0) f[0].focus();
            });
        }
    }

    // ResizeObserver — reposition fixed elements if content changes
    let ro = null;
    if (element.style.position === 'fixed' || element.style.position === 'absolute') {
        ro = new ResizeObserver(() => {
            // no-op: just triggers layout; consumer handles actual reposition
        });
        ro.observe(element);
    }

    // MutationObserver — re-teleport if Blazor snaps element back
    let mo = null;
    if (originalParents.has(element)) {
        const origParent = originalParents.get(element);
        mo = new MutationObserver(() => {
            if (element.parentElement !== target && document.contains(element)) {
                target.appendChild(element);
            }
        });
        mo.observe(target, { childList: true, subtree: false });
        // Also watch original parent in case Blazor inserts a duplicate there
        if (origParent && origParent !== target) {
            mo.observe(origParent, { childList: true, subtree: false });
        }
    }

    portalConfigs.set(element, {
        cfg,
        target,
        focusTrapHandler,
        ro,
        mo,
        prevZIndex: element.style.zIndex,
    });
}

// ── Update portal options; re-teleports if Blazor recreated the element ──
export function update(element, options) {
    const entry = portalConfigs.get(element);
    if (!entry) return;

    // Resolve target (may change via renderAt)
    const renderAt = options?.renderAt || entry.cfg.renderAt || null;
    const target = getTarget(renderAt);

    // Re-teleport if Blazor recreated element in its original position
    if (element.parentElement !== target && document.contains(element)) {
        target.appendChild(element);
    }

    // C# always renders with visibility:hidden — show once we've confirmed teleport
    element.style.visibility = 'visible';

    if (options?.zIndex != null && options.zIndex > 0) {
        element.style.zIndex = options.zIndex;
        entry.cfg.zIndex = options.zIndex;
    }
    if (options?.preventScroll != null) {
        if (options.preventScroll && !entry.cfg.preventScroll) lockScroll();
        else if (!options.preventScroll && entry.cfg.preventScroll) unlockScroll();
        entry.cfg.preventScroll = !!options.preventScroll;
    }

    // Update config
    entry.cfg.renderAt = renderAt;
}

// ── Close / remove portal ──
export function close(element) {
    if (!element) return;
    const entry = portalConfigs.get(element);
    if (!entry) {
        remove(element);
        return;
    }

    // Cleanup observers
    if (entry.ro) { entry.ro.disconnect(); entry.ro = null; }
    if (entry.mo) { entry.mo.disconnect(); entry.mo = null; }

    // Remove focus trap
    if (entry.focusTrapHandler) {
        element.removeEventListener('keydown', entry.focusTrapHandler);
    }

    // Restore focus to previously active element
    if (prevActiveElement && document.contains(prevActiveElement)) {
        try { prevActiveElement.focus(); } catch {}
        prevActiveElement = null;
    }

    // Unlock scroll
    if (entry.cfg.preventScroll) unlockScroll();

    // Hide before moving back to prevent flash in original position
    element.style.visibility = 'hidden';
    // Clear z-index
    element.style.zIndex = entry.prevZIndex || '';

    portalConfigs.delete(element);

    // Move back to original parent
    const originalParent = originalParents.get(element);
    if (originalParent && document.contains(originalParent)) {
        try { originalParent.appendChild(element); }
        catch { try { document.body.removeChild(element); } catch {} }
    } else {
        try { document.body.removeChild(element); } catch {}
    }
}

// ── Legacy teleport (backward compat) ──
export function teleport(element, zIndex) {
    if (!element) return;

    if (!originalParents.has(element)) {
        originalParents.set(element, element.parentNode);
    }

    if (element.parentElement === document.body && element === document.body.lastElementChild) {
        return;
    }

    document.body.appendChild(element);

    if (zIndex != null && zIndex > 0) {
        element.style.zIndex = zIndex;
    }
}

// ── Legacy remove (backward compat) ──
export function remove(element) {
    if (!element) return;
    const entry = portalConfigs.get(element);
    if (entry) {
        close(element);
        return;
    }

    const originalParent = originalParents.get(element);
    if (element.parentNode === document.body) {
        if (originalParent && document.contains(originalParent)) {
            try { originalParent.appendChild(element); }
            catch { try { document.body.removeChild(element); } catch {} }
        } else {
            try { document.body.removeChild(element); } catch {}
        }
    }
    element.style.zIndex = '';
    originalParents.delete(element);
}

// ── Dropdown portal helpers (unchanged) ──
export function teleportDropdown(menuElement, triggerElement) {
    if (!menuElement || !triggerElement) return;
    teleport(menuElement);
    positionDropdown(menuElement, triggerElement);
    portalDropdowns.set(menuElement, triggerElement);
}

export function positionDropdown(menuElement, triggerElement) {
    if (!menuElement || !triggerElement) return;
    var rect = triggerElement.getBoundingClientRect();
    var menuWidth = Math.max(rect.width, 160);
    menuElement.style.position = 'fixed';
    menuElement.style.top = (rect.bottom + 2) + 'px';
    menuElement.style.left = rect.left + 'px';
    menuElement.style.width = menuWidth + 'px';
    menuElement.style.minWidth = menuWidth + 'px';
    menuElement.style.maxWidth = Math.min(800, window.innerWidth - rect.left - 8) + 'px';
}

export function repositionDropdowns() {
    portalDropdowns.forEach(function(trigger, menu) {
        if (document.contains(menu) && document.contains(trigger)) {
            positionDropdown(menu, trigger);
        }
    });
}

window.addEventListener('scroll', repositionDropdowns, true);
window.addEventListener('resize', repositionDropdowns);

window.__superuiDropdown = {
    teleport: teleportDropdown,
    position: positionDropdown,
    remove: removeDropdown
};

export function removeDropdown(menuElement) {
    if (!menuElement) return;
    remove(menuElement);
    portalDropdowns.delete(menuElement);
}
