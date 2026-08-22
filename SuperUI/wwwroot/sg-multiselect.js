export function getScrollTop(id) {
    const el = document.getElementById(id);
    return el ? el.scrollTop : 0;
}
export function scrollToSelected(id) {
    const m = document.getElementById(id);
    if (!m) return;
    const s = m.querySelector('.sgc-selected');
    if (s) {
        const oh = s.offsetTop - m.offsetTop;
        m.scrollTop = oh - 60;
    }
}
export function scrollTo(id, top) {
    const el = document.getElementById(id);
    if (el) el.scrollTo(0, top);
}
export function positionPortal(listboxId, labelId) {
    const m = document.getElementById(listboxId);
    const t = document.querySelector('[aria-labelledby=' + labelId + ']')?.querySelector('.sgc-combo-control');
    if (!m || !t) return;
    const r = t.getBoundingClientRect();
    m.style.position = 'fixed';
    m.style.top = (r.bottom + 2) + 'px';
    m.style.left = r.left + 'px';
    m.style.width = Math.max(r.width, 160) + 'px';
    m.style.minWidth = m.style.width;
    m.style.maxWidth = Math.min(800, innerWidth - r.left - 8) + 'px';
}
export function resetPortal(listboxId) {
    const m = document.getElementById(listboxId);
    if (!m) return;
    m.style.position = '';
    m.style.top = '';
    m.style.left = '';
    m.style.width = '';
    m.style.minWidth = '';
    m.style.maxWidth = '';
}
