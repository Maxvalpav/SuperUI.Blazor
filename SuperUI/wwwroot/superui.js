export function setIndeterminate(el, value) {
    if (el) el.indeterminate = value;
}

/**
 * Auto-resize a textarea to fit its content.
 * @param {HTMLTextAreaElement} el - The textarea element
 * @param {number} minRows - Minimum number of rows
 * @param {number} maxRows - Maximum number of rows (0 = unlimited)
 */
export function autoResizeTextarea(el, minRows, maxRows) {
    if (!el) return;
    const style = getComputedStyle(el);
    const lineHeight = parseFloat(style.lineHeight) || 18;
    const paddingTop = parseFloat(style.paddingTop) || 6;
    const paddingBottom = parseFloat(style.paddingBottom) || 6;
    const minH = (minRows || 1) * lineHeight + paddingTop + paddingBottom;
    el.style.height = 'auto';
    let newH = Math.max(el.scrollHeight, minH);
    if (maxRows > 0) {
        const maxH = maxRows * lineHeight + paddingTop + paddingBottom;
        newH = Math.min(newH, maxH);
        el.style.overflowY = el.scrollHeight > maxH ? 'auto' : 'hidden';
    } else {
        el.style.overflowY = 'hidden';
    }
    el.style.height = newH + 'px';
}

export function init(dotnetRef, gridRoot) {
    if (!gridRoot) return;
    
    // Enable column resize
    const onPointerDown = (e) => {
        const handle = e.target.closest('.sg-resizer');
        if (!handle) return;

        e.preventDefault();
        e.stopPropagation();

        const th = handle.closest('th');
        if (!th) return;

        const key = handle.dataset.colKey;
        const startX = e.clientX;
        const startWidth = th.getBoundingClientRect().width;
        let newWidth = startWidth;
        let lastClientX = startX;
        let resizeRaf = 0;

        th.classList.add('sg-resizing');

        const applyResize = () => {
            resizeRaf = 0;
            newWidth = Math.max(40, startWidth + (lastClientX - startX));
            th.style.width = newWidth + 'px';
            th.style.minWidth = newWidth + 'px';
        };

        const onMove = (ev) => {
            lastClientX = ev.clientX;
            if (resizeRaf) return;
            resizeRaf = window.requestAnimationFrame(applyResize);
        };

        const onUp = () => {
            if (resizeRaf) {
                window.cancelAnimationFrame(resizeRaf);
                applyResize();
            }
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
            th.classList.remove('sg-resizing');
            // Commit to Blazor side.
            try {
                if (!dotnetRef._sgDisposed)
                    dotnetRef.invokeMethodAsync('SetColumnWidthAsync', key, Math.round(newWidth));
            } catch (e) { }
        };

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp, { once: true });
    };

    gridRoot.addEventListener('pointerdown', onPointerDown);

    // Column auto-fit on double click
    const onDblClick = (e) => {
        const handle = e.target.closest('.sg-resizer');
        if (!handle) return;

        const key = handle.dataset.colKey;
        try {
            if (!dotnetRef._sgDisposed)
                dotnetRef.invokeMethodAsync('AutoSizeColumnAsync', key);
        } catch (e) { }
    };

    gridRoot.addEventListener('dblclick', onDblClick);

    // Virtualization scroll support
    const scrollContainer = gridRoot.querySelector('.sg-scroll');
    if (scrollContainer) {
        let scrollRaf = 0;
        const onScroll = () => {
            if (scrollRaf) return;
            scrollRaf = window.requestAnimationFrame(() => {
                scrollRaf = 0;
                try {
                    if (!dotnetRef._sgDisposed) {
                        dotnetRef.invokeMethodAsync('OnScrollAsync', 
                            Math.round(scrollContainer.scrollTop), 
                            Math.round(scrollContainer.clientHeight));
                    }
                } catch (e) { }
            });
        };
        scrollContainer.addEventListener('scroll', onScroll);
        // Initial viewport height
        setTimeout(onScroll, 0);
    }

    let dragKey = null;
    let dragPinned = null;
    let dragOverRaf = 0;
    let dragOverTarget = null;
    let dragOverX = 0;

    const onDragStart = (e) => {
        const th = e.target.closest('th[draggable="true"]');
        if (!th) return;
        if (e.target.closest('.sg-resizer') || e.target.closest('.sg-filter-btn')) {
            e.preventDefault();
            return;
        }
        dragKey = th.dataset.colKey;
        dragPinned = th.dataset.colPinned;
        th.classList.add('sg-dragging');
        if (e.dataTransfer) {
            e.dataTransfer.effectAllowed = 'move';
            try { e.dataTransfer.setData('text/plain', dragKey); } catch { }
        }
    };

    const applyDragOver = () => {
        dragOverRaf = 0;
        const th = dragOverTarget?.closest('th[data-col-key]');
        if (!th || !dragKey || th.dataset.colKey === dragKey) return;
        if (th.dataset.colPinned !== dragPinned) return;

        const rect = th.getBoundingClientRect();
        const before = (dragOverX - rect.left) < rect.width / 2;
        th.classList.remove('sg-drop-before', 'sg-drop-after');
        th.classList.add(before ? 'sg-drop-before' : 'sg-drop-after');
    };

    const onDragOver = (e) => {
        if (!dragKey) return;
        const th = e.target.closest('th[data-col-key]');
        if (!th || th.dataset.colKey === dragKey) return;
        if (th.dataset.colPinned !== dragPinned) return;
        e.preventDefault();
        dragOverTarget = e.target;
        dragOverX = e.clientX;
        if (dragOverRaf) return;
        dragOverRaf = window.requestAnimationFrame(applyDragOver);
    };

    const onDragLeave = (e) => {
        const th = e.target.closest('th[data-col-key]');
        if (th) th.classList.remove('sg-drop-before', 'sg-drop-after');
    };

    const onDrop = (e) => {
        if (!dragKey) return;
        const th = e.target.closest('th[data-col-key]');
        if (!th || th.dataset.colKey === dragKey) return;
        if (th.dataset.colPinned !== dragPinned) return;
        e.preventDefault();
        const rect = th.getBoundingClientRect();
        const before = (e.clientX - rect.left) < rect.width / 2;
        const targetKey = th.dataset.colKey;
        th.classList.remove('sg-drop-before', 'sg-drop-after');
        try {
            if (!dotnetRef._sgDisposed)
                dotnetRef.invokeMethodAsync('ReorderColumnAsync', dragKey, targetKey, before);
        } catch (e) { }
    };

    const onDragEnd = () => {
        if (dragOverRaf) {
            window.cancelAnimationFrame(dragOverRaf);
            dragOverRaf = 0;
        }
        gridRoot.querySelectorAll('.sg-dragging').forEach(el => el.classList.remove('sg-dragging'));
        gridRoot.querySelectorAll('.sg-drop-before, .sg-drop-after')
            .forEach(el => el.classList.remove('sg-drop-before', 'sg-drop-after'));
        dragKey = null;
        dragPinned = null;
        dragOverTarget = null;
    };

    gridRoot.addEventListener('dragstart', onDragStart);
    gridRoot.addEventListener('dragover', onDragOver);
    gridRoot.addEventListener('dragleave', onDragLeave);
    gridRoot.addEventListener('drop', onDrop);
    gridRoot.addEventListener('dragend', onDragEnd);

    // Filter menu anchored positioning: keep it visible even when the
    // scroll container has limited height / overflow:auto.
    const positionFilterMenu = (menu) => {
        const th = menu.closest('th');
        if (!th) {
            return;
        }
        const thRect = th.getBoundingClientRect();
        const menuW = menu.offsetWidth || 280;
        const margin = 4;
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        const isRight = menu.classList.contains('sg-filter-menu--right');
        let left = isRight ? thRect.right - menuW : thRect.left;
        left = Math.max(margin, Math.min(left, vw - menuW - margin));

        // Prefer below; if not enough room, flip above. Cap to whichever side has room.
        const spaceBelow = vh - thRect.bottom - margin - 2;
        const spaceAbove = thRect.top - margin - 2;
        const desired = 450;
        let top, maxH;
        if (spaceBelow >= 220 || spaceBelow >= spaceAbove) {
            top = thRect.bottom + 2;
            maxH = Math.min(desired, spaceBelow);
        } else {
            maxH = Math.min(desired, spaceAbove);
            top = Math.max(margin, thRect.top - 2 - maxH);
        }
        // Final guard: never exceed viewport.
        maxH = Math.max(120, Math.min(maxH, vh - top - margin));

        menu.style.position = 'fixed';
        menu.style.left = left + 'px';
        menu.style.top = top + 'px';
        menu.style.right = 'auto';
        menu.style.maxHeight = maxH + 'px';
        menu.style.height = maxH + 'px';
        menu.style.zIndex = '1000';
        // Write body max-height as CSS custom property — Blazor won't reset it
        // body max = total - footer (~37px) - body top+bottom padding (~12px)
        menu.style.setProperty('--sg-filter-body-h', Math.max(80, maxH - 49) + 'px');
    };

    const repositionAll = () => {
        gridRoot.querySelectorAll('.sg-filter-menu').forEach(positionFilterMenu);
    };

    const menuObserver = new MutationObserver((records) => {
        for (const r of records) {
            r.addedNodes.forEach(n => {
                if (n.nodeType !== 1) return;
                if (n.classList?.contains('sg-filter-menu')) {
                    positionFilterMenu(n);
                }
                n.querySelectorAll?.('.sg-filter-menu').forEach(positionFilterMenu);
            });
        }
    });
    menuObserver.observe(gridRoot, { childList: true, subtree: true });

    window.addEventListener('resize', repositionAll);
    window.addEventListener('scroll', repositionAll, true);

    // Keep handles so DisposeAsync can clean up.
    init._cleanup = init._cleanup || new Map();
    init._cleanup.set(dotnetRef, () => {
        gridRoot.removeEventListener('pointerdown', onPointerDown);
        gridRoot.removeEventListener('dragstart', onDragStart);
        gridRoot.removeEventListener('dragover', onDragOver);
        gridRoot.removeEventListener('dragleave', onDragLeave);
        gridRoot.removeEventListener('drop', onDrop);
        gridRoot.removeEventListener('dragend', onDragEnd);
        menuObserver.disconnect();
        window.removeEventListener('resize', repositionAll);
        window.removeEventListener('scroll', repositionAll, true);
    });
}

export function downloadFile(fileName, contentType, base64Data) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

export function dispose(dotnetRef) {
    const cleanup = init._cleanup?.get(dotnetRef);
    if (cleanup) {
        cleanup();
        init._cleanup.delete(dotnetRef);
    }
    // Null out the ref so any in-flight handlers don't call into disposed .NET object
    if (dotnetRef) dotnetRef._sgDisposed = true;
}

// Auto-fit: measure text width via canvas for each column
// columns: [{key, title, values: [string]}]
// returns: {key: width}
export function measureColumnWidths(columns, gridId) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Scope style sampling to the target grid to avoid cross-grid font mismatch.
    const root = gridId ? document.getElementById(gridId) : null;
    const sampleTd = root?.querySelector('.sg-td') || document.querySelector('.sg-td');
    const sampleTh = root?.querySelector('.sg-table thead th') || document.querySelector('.sg-table thead th');
    const bodyFont = sampleTd
        ? getComputedStyle(sampleTd).font
        : '12px "Segoe UI", Tahoma, Arial, sans-serif';
    const headFont = sampleTh
        ? getComputedStyle(sampleTh).font
        : 'bold 12px "Segoe UI", Tahoma, Arial, sans-serif';

    const PAD = 28; // left + right padding + some breathing room
    const MIN = 50;
    const MAX = 400;

    const result = {};
    for (const col of columns) {
        // Measure header
        ctx.font = headFont;
        let max = ctx.measureText(col.title).width;

        // Measure each value
        ctx.font = bodyFont;
        for (const val of col.values) {
            const w = ctx.measureText(val).width;
            if (w > max) max = w;
        }

        result[col.key] = Math.min(MAX, Math.max(MIN, Math.ceil(max) + PAD));
    }
    return result;
}

export function downloadCsv(fileName, content) {
    // Prepend BOM so Excel opens UTF-8 correctly.
    const blob = new Blob(["\uFEFF" + content], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

export function downloadExcel(fileName, htmlContent) {
    const template = `
        <html xmlns:o="urn:schemas-microsoft-com:office:office" 
              xmlns:x="urn:schemas-microsoft-com:office:excel" 
              xmlns="http://www.w3.org/TR/REC-html40">
        <head>
            <meta charset="utf-8" />
            <!--[if gte mso 9]>
            <xml>
                <x:ExcelWorkbook>
                    <x:ExcelWorksheets>
                        <x:ExcelWorksheet>
                            <x:Name>Sheet1</x:Name>
                            <x:WorksheetOptions>
                                <x:DisplayGridlines/>
                            </x:WorksheetOptions>
                        </x:ExcelWorksheet>
                    </x:ExcelWorksheets>
                </x:ExcelWorkbook>
            </xml>
            <![endif]-->
        </head>
        <body>
            ${htmlContent}
        </body>
        </html>`;

    // Add UTF-8 BOM for Excel to recognize Cyrillic correctly
    const bom = new Uint8Array([0xEF, 0xBB, 0xBF]);
    const blob = new Blob([bom, template], { type: 'application/vnd.ms-excel;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

export function scrollToBottom(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

export function sgDownloadFile(fileName, contentType, base64Data) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
window.sgDownloadFile = sgDownloadFile;

export function isWithin(container) {
    if (!container) return false;
    return container.contains(document.activeElement);
}

export function getLocalStorage(key) {
    try {
        return window.localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function setLocalStorage(key, value) {
    try {
        window.localStorage.setItem(key, value);
        // Dispatch event for same-tab listeners
        window.dispatchEvent(new CustomEvent('sg-storage-updated', { detail: { key, value } }));
    } catch {
        // ignore storage errors
    }
}

export function initStorageListener(dotnetRef) {
    const handler = (e) => {
        if (e.key) { // From other tabs
            dotnetRef.invokeMethodAsync('OnStorageChangedFromJsAsync', e.key, e.newValue);
        } else if (e.detail) { // From same tab
            dotnetRef.invokeMethodAsync('OnStorageChangedFromJsAsync', e.detail.key, e.detail.value);
        }
    };
    window.addEventListener('storage', handler);
    window.addEventListener('sg-storage-updated', handler);
    return {
        dispose: () => {
            window.removeEventListener('storage', handler);
            window.removeEventListener('sg-storage-updated', handler);
        }
    };
}

export function initDataGridVirtualization(dotNetRef, gridRoot) {
    if (!gridRoot) return;
    
    const scrollContainer = gridRoot.querySelector('.sg-scroll');
    if (!scrollContainer) return;
    
    // Measure viewport height
    const viewportHeight = scrollContainer.clientHeight;
    
    // Measure row height after first render
    const firstRow = scrollContainer.querySelector('tbody tr:not([style*="height"])');
    if (firstRow) {
        const rowHeight = firstRow.offsetHeight;
        try {
            if (!dotNetRef._sgDisposed) {
                dotNetRef.invokeMethodAsync('OnRowHeightMeasuredAsync', rowHeight);
            }
        } catch (e) {
            // Ignore errors if component is disposed
        }
    }
    
    // Attach throttled scroll listener (16ms throttle for 60fps)
    let scrollTimeout = null;
    const onScroll = () => {
        if (scrollTimeout) return;
        scrollTimeout = setTimeout(() => {
            const scrollTop = scrollContainer.scrollTop;
            const currentViewportHeight = scrollContainer.clientHeight;
            try {
                if (!dotNetRef._sgDisposed) {
                    dotNetRef.invokeMethodAsync('OnScrollAsync', scrollTop, currentViewportHeight);
                }
            } catch (e) {
                // Ignore errors if component is disposed
            }
            scrollTimeout = null;
        }, 16); // ~60fps
    };
    
    scrollContainer.addEventListener('scroll', onScroll);
    
    // Initial call to set viewport height and initial rows
    onScroll();
    
    // Store cleanup function
    initDataGridVirtualization._cleanup = initDataGridVirtualization._cleanup || new Map();
    initDataGridVirtualization._cleanup.set(dotNetRef, () => {
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }
        scrollContainer.removeEventListener('scroll', onScroll);
    });
}

export function disposeDataGridVirtualization(dotNetRef) {
    const cleanup = initDataGridVirtualization._cleanup?.get(dotNetRef);
    if (cleanup) {
        cleanup();
        initDataGridVirtualization._cleanup.delete(dotNetRef);
    }
}

export function positionFilterMenu(gridRoot, colKey) {
    if (!gridRoot) return;

    function tryPosition() {
        const th = gridRoot.querySelector(`th[data-col-key="${CSS.escape(colKey)}"]`);
        const menu = th ? th.querySelector('.sg-filter-menu') : null;
        if (!menu) return;

        const thRect = th.getBoundingClientRect();

        // Find the scroll container — it clips the menu
        const scroll = gridRoot.querySelector('.sg-scroll');
        const foot = gridRoot.querySelector('.sg-foot');

        // Bottom boundary = top of footer bar (pagination)
        let bottomBoundary;
        if (foot) {
            bottomBoundary = foot.getBoundingClientRect().top;
        } else if (scroll) {
            bottomBoundary = scroll.getBoundingClientRect().bottom;
        } else {
            bottomBoundary = gridRoot.getBoundingClientRect().bottom;
        }

        // Available height: from bottom of th header to bottom boundary
        const availableH = Math.max(150, bottomBoundary - thRect.bottom - 4);
        menu.style.maxHeight = availableH + 'px';
        menu.style.setProperty('--sg-filter-max-h', availableH + 'px');
    }

    // Run immediately and after DOM flush
    tryPosition();
    requestAnimationFrame(() => requestAnimationFrame(tryPosition));
}

