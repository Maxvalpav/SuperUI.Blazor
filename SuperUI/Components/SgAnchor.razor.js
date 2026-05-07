// SgAnchor JavaScript module - ES6 module for Blazor JS isolation
export function init(dotNetRef, selector, offset) {
    if (typeof window === 'undefined') return [];

    // Clean up previous handlers
    if (window.__sgAnchorScrollHandler) {
        window.removeEventListener('scroll', window.__sgAnchorScrollHandler);
    }

    window.__sgAnchorDotNetRef = dotNetRef;
    window.__sgAnchorSelector = selector || 'h2, h3, h4';
    window.__sgAnchorOffset = offset || 80;

    // Scan headers on initialization
    const headers = scanHeaders(window.__sgAnchorSelector);

    // Add scroll handler with throttle
    window.__sgAnchorScrollHandler = throttle(() => {
        updateActiveAnchor();
    }, 100);

    window.addEventListener('scroll', window.__sgAnchorScrollHandler, { passive: true });

    // Initial update
    updateActiveAnchor();

    return headers;
}

export function scanHeaders(selector) {
    if (typeof document === 'undefined') return [];

    const headers = document.querySelectorAll(selector);
    const result = [];

    headers.forEach((header, index) => {
        const id = header.id || generateId(header, index);
        if (!header.id) header.id = id;

        result.push({
            id: id,
            title: header.textContent || '',
            level: parseInt(header.tagName.substring(1)),
            isActive: false
        });
    });

    return result;
}

export function scrollTo(id, offset) {
    if (typeof document === 'undefined') return;

    const element = document.getElementById(id);
    if (!element) return;

    const elementPosition = element.getBoundingClientRect().top + window.pageYOffset;
    const scrollPosition = elementPosition - (offset || 80);

    window.scrollTo({
        top: scrollPosition,
        behavior: 'smooth'
    });
}

export function dispose() {
    if (window.__sgAnchorScrollHandler) {
        window.removeEventListener('scroll', window.__sgAnchorScrollHandler);
        window.__sgAnchorScrollHandler = null;
    }
    window.__sgAnchorDotNetRef = null;
    window.__sgAnchorSelector = null;
    window.__sgAnchorOffset = null;
}

// Helper functions
function updateActiveAnchor() {
    if (!window.__sgAnchorDotNetRef || !window.__sgAnchorSelector) return;

    const headers = document.querySelectorAll(window.__sgAnchorSelector);
    if (!headers.length) return;

    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    const offset = window.__sgAnchorOffset || 80;
    let activeId = null;
    let maxVisible = 0;

    // Find header that is most visible on screen
    headers.forEach((header) => {
        const rect = header.getBoundingClientRect();
        const visibleHeight = Math.min(rect.bottom, window.innerHeight) - Math.max(rect.top, 0);

        if (visibleHeight > maxVisible && rect.top <= offset + 100) {
            maxVisible = visibleHeight;
            activeId = header.id;
        }
    });

    // If not found, take first visible from top
    if (!activeId && headers.length > 0) {
        for (let i = headers.length - 1; i >= 0; i--) {
            if (headers[i].getBoundingClientRect().top <= window.innerHeight) {
                activeId = headers[i].id;
                break;
            }
        }
    }

    if (!activeId && headers.length > 0) {
        activeId = headers[0].id;
    }

    // Calculate scroll progress
    const docHeight = document.documentElement.scrollHeight - window.innerHeight;
    const progress = docHeight > 0 ? Math.min(100, (scrollTop / docHeight) * 100) : 0;

    if (window.__sgAnchorDotNetRef && activeId) {
        window.__sgAnchorDotNetRef.invokeMethodAsync('UpdateActiveAnchor', activeId, progress);
    }
}

function generateId(element, index) {
    const text = element.textContent || '';
    const baseId = text.toLowerCase()
        .replace(/[^\w\s-]/g, '')
        .replace(/\s+/g, '-')
        .substring(0, 50);
    return baseId || `heading-${index}`;
}

function throttle(func, limit) {
    let inThrottle;
    return function () {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => { inThrottle = false; }, limit);
        }
    };
}