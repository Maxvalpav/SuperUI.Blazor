// superui-tooltip.js — Smart positioning for tooltips with portal support
// Supports: offset, followCursor, interactive content, scroll/resize repositioning

const tooltipInstances = new WeakMap();

export function attach(trigger, tooltip, placement, dotnet, offset = 8, followCursor = false, interactive = false) {
    if (!trigger) return;

    const existing = tooltipInstances.get(trigger);
    if (existing) existing.dispose();

    let isDisposed = false;

    const handleTriggerBlur = () => {
        if (!isDisposed && dotnet) {
            try { dotnet.invokeMethodAsync('HideFromJsAsync').catch(() => {}); } catch { }
        }
    };

    let cursorX = 0, cursorY = 0;

    const handleMouseMove = (e) => {
        cursorX = e.clientX;
        cursorY = e.clientY;
        if (tooltip && tooltip.style && tooltip.style.display !== 'none' && tooltip.style.opacity !== '0') {
            positionTooltip(trigger, tooltip, placement, offset, cursorX, cursorY, followCursor);
        }
    };

    // For Interactive mode: keep tooltip open when mouse enters it
    let tooltipTimer = null;
    const clearTooltipTimer = () => { if (tooltipTimer) { clearTimeout(tooltipTimer); tooltipTimer = null; } };

    const handleTooltipMouseEnter = () => {
        clearTooltipTimer();
    };

    const handleTooltipMouseLeave = () => {
        clearTooltipTimer();
        tooltipTimer = setTimeout(() => {
            if (!isDisposed && dotnet) {
                try { dotnet.invokeMethodAsync('HideFromJsAsync').catch(() => {}); } catch { }
            }
        }, 100);
    };

    // Scroll repositioning
    let scrollParents = [];
    const findScrollParents = (el) => {
        const parents = [];
        let parent = el.parentElement;
        while (parent && parent !== document.body) {
            const style = getComputedStyle(parent);
            if (style.overflow === 'auto' || style.overflow === 'scroll' ||
                style.overflowY === 'auto' || style.overflowY === 'scroll' ||
                style.overflowX === 'auto' || style.overflowX === 'scroll') {
                parents.push(parent);
            }
            parent = parent.parentElement;
        }
        parents.push(window);
        return parents;
    };

    const reposition = () => {
        if (!tooltip || !tooltip.style || tooltip.style.display === 'none') return;
        if (followCursor) {
            positionTooltip(trigger, tooltip, placement, offset, cursorX, cursorY, true);
        } else {
            positionTooltip(trigger, tooltip, placement, offset);
        }
    };

    trigger.addEventListener('blur', handleTriggerBlur);

    if (followCursor) {
        trigger.addEventListener('mousemove', handleMouseMove);
    }

    if (interactive && tooltip) {
        tooltip.addEventListener('mouseenter', handleTooltipMouseEnter);
        tooltip.addEventListener('mouseleave', handleTooltipMouseLeave);
    }

    scrollParents = findScrollParents(trigger);
    scrollParents.forEach(p => p.addEventListener('scroll', reposition, { passive: true }));

    const resizeObserver = new ResizeObserver(() => reposition());
    resizeObserver.observe(document.body);

    const instance = {
        trigger, tooltip, placement, dotnet, offset, followCursor, interactive,
        scrollParents, resizeObserver, reposition,
        isDisposed: false,
        handleTriggerBlur, handleMouseMove, cursorX, cursorY,
        handleTooltipMouseEnter, handleTooltipMouseLeave,
        clearTooltipTimer, tooltipTimer,
        dispose: () => {
            isDisposed = true;
            if (trigger) {
                trigger.removeEventListener('blur', handleTriggerBlur);
                trigger.removeEventListener('mousemove', handleMouseMove);
            }
            if (tooltip) {
                tooltip.removeEventListener('mouseenter', handleTooltipMouseEnter);
                tooltip.removeEventListener('mouseleave', handleTooltipMouseLeave);
            }
            scrollParents.forEach(p => p.removeEventListener('scroll', reposition));
            resizeObserver.disconnect();
            clearTooltipTimer();
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

export function show(trigger, tooltip, placement = 'top', zIndex, offset = 8) {
    if (!trigger || !tooltip || !tooltip.style) return;

    const instance = tooltipInstances.get(trigger);

    if (zIndex) tooltip.style.zIndex = zIndex;

    tooltip.style.display = 'block';
    tooltip.style.opacity = '0';
    tooltip.style.visibility = 'hidden';

    requestAnimationFrame(() => {
        if (!tooltip || !tooltip.style) return;
        if (instance && instance.followCursor) {
            positionTooltip(trigger, tooltip, placement, offset, instance.cursorX, instance.cursorY, true);
        } else {
            positionTooltip(trigger, tooltip, placement, offset);
        }
        tooltip.style.visibility = 'visible';
        tooltip.style.opacity = '1';
    });
}

export function hide(tooltip) {
    if (!tooltip || !tooltip.style) return;
    tooltip.style.opacity = '0';
    setTimeout(() => {
        if (tooltip && tooltip.style && tooltip.style.opacity === '0') {
            tooltip.style.display = 'none';
        }
    }, 150);
}

function positionTooltip(trigger, tooltip, placement = 'top', offset = 8, cursorX, cursorY, followCursor = false) {
    if (!trigger || !tooltip || !tooltip.style) return;

    const tooltipRect = tooltip.getBoundingClientRect();
    if (tooltipRect.width === 0 && tooltipRect.height === 0) return; // Hidden or detached

    const vw = window.innerWidth;
    const vh = window.innerHeight;

    const parts = placement.split('-');
    const dir = parts[0];
    const align = parts[1] || 'center';

    let top, left;

    if (followCursor && cursorX !== undefined && cursorY !== undefined) {
        // Position relative to cursor with offset
        if (dir === 'bottom') {
            top = cursorY + offset;
            left = cursorX - (tooltipRect.width / 2);
        } else if (dir === 'left') {
            left = cursorX - tooltipRect.width - offset;
            top = cursorY - (tooltipRect.height / 2);
        } else if (dir === 'right') {
            left = cursorX + offset;
            top = cursorY - (tooltipRect.height / 2);
        } else { // top
            top = cursorY - tooltipRect.height - offset;
            left = cursorX - (tooltipRect.width / 2);
        }
    } else {
        const triggerRect = trigger.getBoundingClientRect();
        if (triggerRect.width === 0 && triggerRect.height === 0) return; // Trigger is hidden/detached

        if (dir === 'bottom') {
            top = triggerRect.bottom + offset;
            if (align === 'start') left = triggerRect.left;
            else if (align === 'end') left = triggerRect.right - tooltipRect.width;
            else left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
        } else if (dir === 'left') {
            left = triggerRect.left - tooltipRect.width - offset;
            if (align === 'start') top = triggerRect.top;
            else if (align === 'end') top = triggerRect.bottom - tooltipRect.height;
            else top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
        } else if (dir === 'right') {
            left = triggerRect.right + offset;
            if (align === 'start') top = triggerRect.top;
            else if (align === 'end') top = triggerRect.bottom - tooltipRect.height;
            else top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
        } else { // top
            top = triggerRect.top - tooltipRect.height - offset;
            if (align === 'start') left = triggerRect.left;
            else if (align === 'end') left = triggerRect.right - tooltipRect.width;
            else left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
        }
    }

    // ── Viewport collision detection (auto-flip) ──
    const getTriggerRect = () => trigger.getBoundingClientRect();

    if (dir === 'top' && top < 4) {
        top = followCursor
            ? (cursorY !== undefined ? cursorY + offset : (getTriggerRect().bottom + offset))
            : (getTriggerRect().bottom + offset);
    } else if (dir === 'bottom' && top + tooltipRect.height > vh - 4) {
        top = followCursor
            ? (cursorY !== undefined ? cursorY - tooltipRect.height - offset : (getTriggerRect().top - tooltipRect.height - offset))
            : (getTriggerRect().top - tooltipRect.height - offset);
    } else if (dir === 'left' && left < 4) {
        left = followCursor
            ? (cursorX !== undefined ? cursorX + offset : (getTriggerRect().right + offset))
            : (getTriggerRect().right + offset);
    } else if (dir === 'right' && left + tooltipRect.width > vw - 4) {
        left = followCursor
            ? (cursorX !== undefined ? cursorX - tooltipRect.width - offset : (getTriggerRect().left - tooltipRect.width - offset))
            : (getTriggerRect().left - tooltipRect.width - offset);
    }

    // ── Clamp within viewport with padding ──
    left = Math.max(4, Math.min(left, vw - tooltipRect.width - 4));
    top = Math.max(4, Math.min(top, vh - tooltipRect.height - 4));

    if (tooltip.style) {
        tooltip.style.top = `${top}px`;
        tooltip.style.left = `${left}px`;
    }
}
