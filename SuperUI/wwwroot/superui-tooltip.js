// superui-tooltip.js - Smart positioning for tooltips with portal support
// Uses WeakMap to support multiple tooltips on a page

const tooltipInstances = new WeakMap();

export function attach(trigger, tooltip, placement = 'top', dotnet) {
    // Cleanup previous instance on same trigger
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
    
    if (trigger) {
        trigger.addEventListener('blur', handleTriggerBlur);
    }
    
    const instance = {
        trigger,
        tooltip,
        placement,
        dotnet,
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

export function show(trigger, tooltip, placement = 'top') {
    if (!trigger || !tooltip) return;

    tooltip.style.display = 'block';
    tooltip.style.visibility = 'hidden';
    tooltip.style.opacity = '0';
    
    const triggerRect = trigger.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    const padding = 8;

    let top, left;

    const positions = {
        top: {
            top: triggerRect.top - tooltipRect.height - padding,
            left: triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2)
        },
        bottom: {
            top: triggerRect.bottom + padding,
            left: triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2)
        },
        left: {
            top: triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2),
            left: triggerRect.left - tooltipRect.width - padding
        },
        right: {
            top: triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2),
            left: triggerRect.right + padding
        }
    };

    let pos = positions[placement] || positions.top;

    // Viewport collision detection and auto-flip
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    if (placement === 'top' && pos.top < padding) {
        pos = positions.bottom;
    } else if (placement === 'bottom' && pos.top + tooltipRect.height > vh - padding) {
        pos = positions.top;
    } else if (placement === 'left' && pos.left < padding) {
        pos = positions.right;
    } else if (placement === 'right' && pos.left + tooltipRect.width > vw - padding) {
        pos = positions.left;
    }

    // Final boundary check (clamping) - keep within viewport
    left = Math.max(padding, Math.min(pos.left, vw - tooltipRect.width - padding));
    top = Math.max(padding, Math.min(pos.top, vh - tooltipRect.height - padding));

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
