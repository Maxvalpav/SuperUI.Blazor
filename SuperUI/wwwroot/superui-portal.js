// superui-portal.js - Simple teleport to body end with safe cleanup

const originalParents = new WeakMap();

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
