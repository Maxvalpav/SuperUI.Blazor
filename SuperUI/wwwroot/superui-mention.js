// ─── SuperUI Mention JS Module ─────────────────────────────────────────────

export function init(input, overlay, dotNetRef, triggers, debounceMs) {
    if (!input || !overlay) return;

    const triggersSet = new Set(triggers || ['@']);
    const debounce = debounceMs || 200;
    let debounceTimer = null;
    let active = false;               // dropdown visible
    let currentTrigger = null;        // the trigger char
    let searchStart = -1;             // index where trigger was found

    // ── Measure cursor pixel position ──
    function getCursorPixelPos() {
        const pos = input.selectionStart;
        const style = getComputedStyle(input);

        const mirror = document.createElement('div');
        mirror.style.cssText = `
            position: fixed; top: -9999px; left: -9999px;
            white-space: pre-wrap; word-wrap: break-word;
            overflow-wrap: break-word; visibility: hidden;
            font: ${style.font};
            font-size: ${style.fontSize};
            font-family: ${style.fontFamily};
            letter-spacing: ${style.letterSpacing};
            line-height: ${style.lineHeight};
            padding: ${style.padding};
            border: ${style.border};
            box-sizing: border-box;
            width: ${input.clientWidth}px;
        `;

        const text = input.value.substring(0, pos);
        const textNode = document.createTextNode(text);
        mirror.appendChild(textNode);

        // Add a marker span at the cursor position
        const marker = document.createElement('span');
        marker.id = 'sg-mention-cursor-marker';
        marker.textContent = '|';
        mirror.appendChild(marker);

        // Append rest of text after marker to maintain wrapping
        const restNode = document.createTextNode(input.value.substring(pos));
        mirror.appendChild(restNode);

        document.body.appendChild(mirror);
        mirror.scrollTop = input.scrollTop;

        const markerRect = marker.getBoundingClientRect();
        const inputRect = input.getBoundingClientRect();

        document.body.removeChild(mirror);

        return {
            x: markerRect.left - inputRect.left,
            y: markerRect.top - inputRect.top - input.scrollTop,
            h: markerRect.height
        };
    }

    // ── Position overlay ──
    function positionOverlay() {
        const pos = getCursorPixelPos();
        const inputRect = input.getBoundingClientRect();

        overlay.style.position = 'fixed';
        overlay.style.left = `${inputRect.left + pos.x}px`;
        overlay.style.top = `${inputRect.top + pos.y + pos.h}px`;
        overlay.style.minWidth = '200px';
        overlay.style.display = 'block';
    }

    // ── Show overlay ──
    function showOverlay(trigger, startIdx) {
        currentTrigger = trigger;
        searchStart = startIdx;
        active = true;
        positionOverlay();
        overlay.style.display = 'block';
    }

    // ── Hide overlay ──
    function hideOverlay() {
        active = false;
        currentTrigger = null;
        searchStart = -1;
        overlay.style.display = 'none';
    }

    // ── Check for trigger ──
    function checkTrigger() {
        const pos = input.selectionStart;
        const text = input.value;
        if (pos <= 0) return;

        const charBefore = text[pos - 1];

        // Check if char before cursor is a trigger
        // Also check if we're continuing a search (previous chars also trigger chars)
        if (triggersSet.has(charBefore)) {
            // Found trigger, start new search
            showOverlay(charBefore, pos);
            // Notify Blazor with empty search
            try { dotNetRef.invokeMethodAsync('OnSearchChangedJs', ''); } catch {}
            return;
        }

        // If overlay is already active, update search
        if (active && currentTrigger && searchStart >= 0) {
            const searchText = text.substring(searchStart, pos);
            try { dotNetRef.invokeMethodAsync('OnSearchChangedJs', searchText); } catch {}
            positionOverlay();
        }
    }

    // ── Input handler ──
    function onInput() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            checkTrigger();
        }, debounce);
    }

    // ── Keydown handler ──
    function onKeyDown(e) {
        if (!active) return;

        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            try { dotNetRef.invokeMethodAsync('OnNavigateJs', e.key === 'ArrowDown' ? 1 : -1); } catch {}
            return;
        }

        if (e.key === 'Enter' || e.key === 'Tab') {
            if (e.key === 'Enter') e.preventDefault();
            try { dotNetRef.invokeMethodAsync('OnSelectJs'); } catch {}
            return;
        }

        if (e.key === 'Escape') {
            e.preventDefault();
            hideOverlay();
            try { dotNetRef.invokeMethodAsync('OnCloseJs'); } catch {}
            return;
        }

        // If Backspace is pressed and we'd go before the trigger, close
        if (e.key === 'Backspace' && active && currentTrigger && searchStart >= 0) {
            const pos = input.selectionStart;
            // Allow the backspace to happen naturally, then re-check on input
        }
    }

    // ── Blur handler (close if clicking outside) ──
    function onBlur() {
        // Delay to allow click on dropdown item to register
        setTimeout(() => {
            if (active) {
                hideOverlay();
                try { dotNetRef.invokeMethodAsync('OnCloseJs'); } catch {}
            }
        }, 200);
    }

    // Register events
    input.addEventListener('input', onInput);
    input.addEventListener('keydown', onKeyDown);
    input.addEventListener('blur', onBlur);

    // ── Public API ──

    input._insertMention = (displayValue, replaceStart, replaceEnd) => {
        const before = input.value.substring(0, replaceStart);
        const after = input.value.substring(replaceEnd);
        const mention = displayValue; // The full mention text like "@username"
        input.value = before + mention + after;

        // Set cursor after the inserted mention
        const newPos = replaceStart + mention.length;
        input.setSelectionRange(newPos, newPos);
        input.focus();

        hideOverlay();

        // Trigger input event for Blazor binding
        input.dispatchEvent(new Event('input', { bubbles: true }));
    };

    input._positionOverlay = () => {
        if (active) positionOverlay();
    };

    input._hideOverlay = () => {
        hideOverlay();
    };

    // Reposition on scroll
    const scrollParent = findScrollParent(input);
    if (scrollParent) {
        scrollParent.addEventListener('scroll', () => {
            if (active) positionOverlay();
        }, { passive: true });
    }

    function findScrollParent(el) {
        while (el && el.parentElement) {
            el = el.parentElement;
            const style = getComputedStyle(el);
            if (style.overflow === 'auto' || style.overflow === 'scroll' ||
                style.overflowY === 'auto' || style.overflowY === 'scroll') {
                return el;
            }
        }
        return null;
    }

    // ── Dispose ──
    input._dispose = () => {
        input.removeEventListener('input', onInput);
        input.removeEventListener('keydown', onKeyDown);
        input.removeEventListener('blur', onBlur);
        hideOverlay();
        input._insertMention = null;
        input._positionOverlay = null;
        input._hideOverlay = null;
        input._dispose = null;
    };
}

// ── Exported wrappers ───────────────────────────────────────────────────────

export function insertMention(input, displayValue, replaceStart, replaceEnd) {
    if (input?._insertMention)
        input._insertMention(displayValue, replaceStart, replaceEnd);
}

export function positionOverlay(input) {
    if (input?._positionOverlay) input._positionOverlay();
}

export function hideOverlay(input) {
    if (input?._hideOverlay) input._hideOverlay();
}

export function dispose(input) {
    if (input?._dispose) input._dispose();
}
