// superui-tooltip.js - Smart positioning for tooltips with portal support

let attachedTooltip = null;
let dotnetRef = null;
let isDisposed = false;

export function attach(trigger, tooltip, placement = 'top', dotnet) {
    isDisposed = false;
    attachedTooltip = { trigger, tooltip, placement };
    dotnetRef = dotnet;
    
    // Add focus loss handler to trigger element
    if (trigger) {
        trigger.addEventListener('blur', handleTriggerBlur);
    }
}

export function detach() {
    isDisposed = true;
    if (attachedTooltip?.trigger) {
        attachedTooltip.trigger.removeEventListener('blur', handleTriggerBlur);
    }
    attachedTooltip = null;
    dotnetRef = null;
}

function handleTriggerBlur() {
    if (!isDisposed && dotnetRef) {
        try {
            dotnetRef.invokeMethodAsync('HideFromJsAsync').catch(() => {});
        } catch { }
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
