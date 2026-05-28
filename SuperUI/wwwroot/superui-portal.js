// superui-portal.js - Teleport elements to body end with safe cleanup

const originalParents = new WeakMap();
const portalDropdowns = new Map();

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

export function remove(element) {
    if (!element) return;

    const originalParent = originalParents.get(element);

    if (element.parentNode === document.body) {
        if (originalParent && document.contains(originalParent)) {
            try {
                originalParent.appendChild(element);
            } catch (e) {
                try { document.body.removeChild(element); } catch (e2) {}
            }
        } else {
            try { document.body.removeChild(element); } catch (e2) {}
        }
    }

    element.style.zIndex = '';
    originalParents.delete(element);
}

// ── Dropdown portal helpers ──

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
