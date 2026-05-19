export function syncHeaderScroll(headerEl, bodyEl) {
    if (headerEl && bodyEl) {
        headerEl.scrollLeft = bodyEl.scrollLeft;
    }
}
