export function syncHeaderScroll(headerEl, bodyEl) {
    if (headerEl && bodyEl) {
        headerEl.scrollLeft = bodyEl.scrollLeft;
    }
}

export function downloadFile(base64, fileName, mimeType) {
    const binary = atob(base64);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

let resizeState = null;

export function startColumnResize(startX, startWidth) {
    if (resizeState) return;
    resizeState = { startX, startWidth };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
}

function onMouseMove(e) {
    if (!resizeState) return;
    resizeState.currentEvent = { clientX: e.clientX };
}

function onMouseUp() {
    if (!resizeState) return;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
    resizeState.end = true;
}

export function pollResizeEvent() {
    if (!resizeState) return null;
    const evt = resizeState.currentEvent || null;
    resizeState.currentEvent = null;
    return evt ? { clientX: evt.clientX } : null;
}

export function pollResizeEnd() {
    if (!resizeState) return false;
    return resizeState.end || false;
}

export function resetResizeState() {
    if (!resizeState) return;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
    resizeState = null;
}
