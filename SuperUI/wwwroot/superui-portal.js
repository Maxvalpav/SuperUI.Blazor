// superui-portal.js - Simple teleport to body end

export function teleport(element) {
    if (!element) return;
    document.body.appendChild(element);
}

export function remove(element) {
    if (element && element.parentNode) {
        element.parentNode.removeChild(element);
    }
}
