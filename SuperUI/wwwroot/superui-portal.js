// superui-portal.js - Simple teleport to body end with safe cleanup

const originalParents = new WeakMap();
const portalDropdowns = new Map();

export function teleport(element) {
    if (!element) return;
    
    // Store original parent to teleport back during cleanup.
    // This prevents Blazor's renderer from crashing with "removeChild" on null parent.
    if (!originalParents.has(element)) {
        originalParents.set(element, element.parentNode);
    }
    
    // If already in body, don't re-append unless it's not the last child
    if (element.parentElement === document.body && element === document.body.lastElementChild) {
        return;
    }
    
    document.body.appendChild(element);
}

export function remove(element) {
    if (!element) return;

    const originalParent = originalParents.get(element);
    
    // If the element is currently in the body, we need to handle it.
    if (element.parentNode === document.body) {
        // Try to move it back to its original position if Blazor hasn't removed the parent yet.
        if (originalParent && document.contains(originalParent)) {
            try {
                originalParent.appendChild(element);
            } catch (e) {
                // If appending back fails, just remove from body
                try { document.body.removeChild(element); } catch (e2) {}
            }
        } else {
            // Original parent is gone or wasn't tracked, just remove from body safely
            try { document.body.removeChild(element); } catch (e2) {}
        }
    }
    
    originalParents.delete(element);
}

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

// Reposition on scroll and resize
window.addEventListener('scroll', repositionDropdowns, true);
window.addEventListener('resize', repositionDropdowns);

// Global API for non-module consumers
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
