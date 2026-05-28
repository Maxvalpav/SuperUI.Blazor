// superui-tooltip.js — Smart positioning for tooltips with portal support
// Uses WeakMap to support multiple tooltips on a page

const tooltipInstances = new WeakMap();
const padding = 8;

export function attach(trigger, tooltip, placement = 'top', dotnet) {
    if (!trigger) return;

    const existing = tooltipInstances.get(trigger);
    if (existing) {
        existing.dispose();
    }

    let isDisposed = false;

    const handleTriggerBlur = () => {
        if (!isDisposed && dotnet) {
            try {
                dotnet.invokeMethodAsync('HideFromJsAsync').catch(() => {});
            } catch { }
        }
    };

    trigger.addEventListener('blur', handleTriggerBlur);

    const instance = {
        trigger, tooltip, placement, dotnet,
        isDisposed: false,
        handleTriggerBlur,
        dispose: () => {
            isDisposed = true;
            if (trigger) {
                trigger.removeEventListener('blur', handleTriggerBlur);
            }
        }
    };

    tooltipInstances.set(trigger, instance);
}

export function detach(trigger) {
    const instance = tooltipInstances.get(trigger);
    if (instance) {
        instance.dispose();
        tooltipInstances.delete(trigger);
    }
}

export function show(trigger, tooltip, placement = 'top', zIndex) {
    if (!trigger || !tooltip) return;

    if (zIndex) {
        tooltip.style.zIndex = zIndex;
    }

    // Reset for measurement
    tooltip.style.display = 'block';
    tooltip.style.visibility = 'hidden';
    tooltip.style.opacity = '0';

    const triggerRect = trigger.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    // Parse placement into direction + alignment
    const parts = placement.split('-');
    const dir = parts[0]; // top, bottom, left, right
    const align = parts[1] || 'center'; // start, center, end

    let top, left;

    // Base position
    if (dir === 'bottom') {
        top = triggerRect.bottom + padding;
        if (align === 'start') {
            left = triggerRect.left;
        } else if (align === 'end') {
            left = triggerRect.right - tooltipRect.width;
        } else {
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
        }
    } else if (dir === 'left') {
        left = triggerRect.left - tooltipRect.width - padding;
        if (align === 'start') {
            top = triggerRect.top;
        } else if (align === 'end') {
            top = triggerRect.bottom - tooltipRect.height;
        } else {
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
        }
    } else if (dir === 'right') {
        left = triggerRect.right + padding;
        if (align === 'start') {
            top = triggerRect.top;
        } else if (align === 'end') {
            top = triggerRect.bottom - tooltipRect.height;
        } else {
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
        }
    } else { // top (default)
        top = triggerRect.top - tooltipRect.height - padding;
        if (align === 'start') {
            left = triggerRect.left;
        } else if (align === 'end') {
            left = triggerRect.right - tooltipRect.width;
        } else {
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
        }
    }

    // ── Viewport collision detection (auto-flip) ──
    if (dir === 'top' && top < padding) {
        top = triggerRect.bottom + padding;
    } else if (dir === 'bottom' && top + tooltipRect.height > vh - padding) {
        top = triggerRect.top - tooltipRect.height - padding;
    } else if (dir === 'left' && left < padding) {
        left = triggerRect.right + padding;
    } else if (dir === 'right' && left + tooltipRect.width > vw - padding) {
        left = triggerRect.left - tooltipRect.width - padding;
    }

    // ── Clamp within viewport ──
    left = Math.max(padding, Math.min(left, vw - tooltipRect.width - padding));
    top = Math.max(padding, Math.min(top, vh - tooltipRect.height - padding));

    tooltip.style.top = `${top}px`;
    tooltip.style.left = `${left}px`;
    tooltip.style.visibility = 'visible';
    tooltip.style.opacity = '1';
}

export function hide(tooltip) {
    if (!tooltip) return;
    tooltip.style.opacity = '0';
    setTimeout(() => {
        if (tooltip.style.opacity === '0') {
            tooltip.style.display = 'none';
        }
    }, 150);
}
