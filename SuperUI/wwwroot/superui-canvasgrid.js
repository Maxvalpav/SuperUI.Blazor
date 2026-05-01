const FONT_HEADER = 'bold 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial';
const FONT_CELL = '13px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial';
const FONT_GROUP = 'bold 13px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial';
const FONT_GROUP_AGG = 'italic 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial';

export function init(canvas, container, dotNet) {
    if (canvas._sgGrid) {
        canvas._sgGrid.dotNet = dotNet;
        return;
    }

    const ctx = canvas.getContext('2d');
    const state = {
        canvas,
        ctx,
        container,
        dotNet,
        data: [],
        columns: [],
        pinnedColumns: [],
        normalColumns: [],
        pinnedWidth: 0,
        totalWidth: 0,
        rowHeight: 35,
        headerHeight: 40,
        scrollTop: 0,
        scrollLeft: 0,
        width: 0,
        height: 0,
        dpr: window.devicePixelRatio || 1,
        hoveredRowIndex: -1,
        resizingColumn: null,
        resizeStartX: 0,
        resizeStartWidth: 0,
        columnFilters: {},
        aggregates: {},
        groupBy: [],
        sortProperty: null,
        sortDescending: false,
        allRowsSelected: false,
        rafPending: false,
        listeners: [],
        columnKeys: [],
        columnIndex: {},
        selectedSlot: -1
    };

    const scrollV = container.querySelector('.sg-canvas-grid-scroll-v');
    const scrollH = container.querySelector('.sg-canvas-grid-scroll-h');

    const recomputeColumnLayout = () => {
        const pinned = [];
        const normal = [];
        let pw = 0;
        let tw = 0;
        for (const c of state.columns) {
            const w = c.width || 0;
            tw += w;
            if (c.pinned) { pinned.push(c); pw += w; }
            else normal.push(c);
        }
        state.pinnedColumns = pinned;
        state.normalColumns = normal;
        state.pinnedWidth = pw;
        state.totalWidth = tw;
    };

    const scheduleRender = () => {
        if (state.rafPending) return;
        state.rafPending = true;
        requestAnimationFrame(() => {
            state.rafPending = false;
            render(state);
        });
    };
    state.scheduleRender = scheduleRender;

    const resize = () => {
        const newDpr = window.devicePixelRatio || 1;
        state.dpr = newDpr;
        state.width = container.clientWidth;
        state.height = container.clientHeight;
        canvas.width = Math.max(1, Math.round(state.width * newDpr));
        canvas.height = Math.max(1, Math.round(state.height * newDpr));
        canvas.style.width = state.width + 'px';
        canvas.style.height = state.height + 'px';
        ctx.setTransform(newDpr, 0, 0, newDpr, 0, 0);
        clampScroll();
        scheduleRender();
    };

    const clampScroll = () => {
        const totalH = state.data.length * state.rowHeight;
        const viewH = Math.max(0, state.height - state.headerHeight - getFooterHeight(state));
        const maxV = Math.max(0, totalH - viewH);
        if (state.scrollTop > maxV) state.scrollTop = maxV;
        if (state.scrollTop < 0) state.scrollTop = 0;

        const maxH = Math.max(0, state.totalWidth - (state.width - state.pinnedWidth));
        if (state.scrollLeft > maxH) state.scrollLeft = maxH;
        if (state.scrollLeft < 0) state.scrollLeft = 0;

        if (scrollV && scrollV.scrollTop !== state.scrollTop) scrollV.scrollTop = state.scrollTop;
        if (scrollH && scrollH.scrollLeft !== state.scrollLeft) scrollH.scrollLeft = state.scrollLeft;
    };

    const onScrollV = () => {
        if (!scrollV) return;
        if (state.scrollTop === scrollV.scrollTop) return;
        state.scrollTop = scrollV.scrollTop;
        scheduleRender();
    };
    const onScrollH = () => {
        if (!scrollH) return;
        if (state.scrollLeft === scrollH.scrollLeft) return;
        state.scrollLeft = scrollH.scrollLeft;
        scheduleRender();
    };
    if (scrollV) {
        scrollV.addEventListener('scroll', onScrollV, { passive: true });
        state.listeners.push(() => scrollV.removeEventListener('scroll', onScrollV));
    }
    if (scrollH) {
        scrollH.addEventListener('scroll', onScrollH, { passive: true });
        state.listeners.push(() => scrollH.removeEventListener('scroll', onScrollH));
    }

    const getMousePos = (e) => {
        const rect = canvas.getBoundingClientRect();
        return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };

    const findColumnAt = (x, y) => {
        if (y >= state.headerHeight) return null;
        const { pinnedColumns, normalColumns, pinnedWidth, scrollLeft, width } = state;

        let cx = 0;
        for (const col of pinnedColumns) {
            if (x >= cx && x < cx + col.width) return { col, drawX: cx };
            cx += col.width;
        }
        cx = pinnedWidth;
        for (const col of normalColumns) {
            const drawX = cx - scrollLeft;
            if (x >= drawX && x < drawX + col.width && drawX + col.width > pinnedWidth && drawX < width) {
                return { col, drawX };
            }
            cx += col.width;
        }
        return null;
    };

    const findResizeAt = (x, y) => {
        if (y >= state.headerHeight) return null;
        const { pinnedColumns, normalColumns, pinnedWidth, scrollLeft, width } = state;

        let cx = 0;
        for (const col of pinnedColumns) {
            if (x > cx + col.width - 5 && x < cx + col.width + 5) return col;
            cx += col.width;
        }
        cx = pinnedWidth;
        for (const col of normalColumns) {
            const drawX = cx - scrollLeft;
            if (x > drawX + col.width - 5 && x < drawX + col.width + 5 && drawX + col.width > pinnedWidth && drawX < width) {
                return col;
            }
            cx += col.width;
        }
        return null;
    };

    const onMouseMove = (e) => {
        const { x, y } = getMousePos(e);

        if (state.resizingColumn) {
            const deltaX = x - state.resizeStartX;
            state.resizingColumn.width = Math.max(40, state.resizeStartWidth + deltaX);
            recomputeColumnLayout();
            if (scrollH) {
                const inner = scrollH.querySelector('div');
                if (inner) inner.style.width = state.totalWidth + 'px';
            }
            scheduleRender();
            return;
        }

        canvas.style.cursor = findResizeAt(x, y) ? 'col-resize' : 'default';

        if (y >= state.headerHeight) {
            const relativeY = y - state.headerHeight + state.scrollTop;
            const rowIndex = Math.floor(relativeY / state.rowHeight);
            const next = (rowIndex >= 0 && rowIndex < state.data.length) ? rowIndex : -1;
            if (next !== state.hoveredRowIndex) {
                state.hoveredRowIndex = next;
                scheduleRender();
            }
        } else if (state.hoveredRowIndex !== -1) {
            state.hoveredRowIndex = -1;
            scheduleRender();
        }
    };

    const onMouseDown = (e) => {
        const { x, y } = getMousePos(e);
        const col = findResizeAt(x, y);
        if (col) {
            state.resizingColumn = col;
            state.resizeStartX = x;
            state.resizeStartWidth = col.width;
            e.preventDefault();
        }
    };

    const onMouseUp = () => {
        if (state.resizingColumn) {
            state.resizingColumn = null;
            if (!state.dotNet) return;
            try {
                state.dotNet.invokeMethodAsync('OnColumnResized', state.columns
                    .filter(c => c.property !== '__selection')
                    .map(c => ({ property: c.property, width: c.width })));
            } catch { }
        }
    };

    const onPointerDown = (e) => {
        if (e.pointerType === 'touch') {
            state._touchStart = { x: e.clientX, y: e.clientY, scrollTop: state.scrollTop, scrollLeft: state.scrollLeft };
        }
        onMouseDown(e);
    };

    const onPointerMove = (e) => {
        if (e.pointerType === 'touch' && state._touchStart && !state.resizingColumn) {
            const dx = state._touchStart.x - e.clientX;
            const dy = state._touchStart.y - e.clientY;
            
            const newScrollTop = state._touchStart.scrollTop + dy;
            const newScrollLeft = state._touchStart.scrollLeft + dx;
            
            if (scrollV) scrollV.scrollTop = newScrollTop;
            if (scrollH) scrollH.scrollLeft = newScrollLeft;
            
            e.preventDefault();
            return;
        }
        onMouseMove(e);
    };

    const onPointerUp = (e) => {
        state._touchStart = null;
        onMouseUp(e);
    };

    const onMouseLeave = () => {
        if (state.hoveredRowIndex !== -1) {
            state.hoveredRowIndex = -1;
            scheduleRender();
        }
    };

    const onClick = (e) => {
        if (state.resizingColumn) return;
        if (!state.dotNet) return;
        const { x, y } = getMousePos(e);

        if (y < state.headerHeight) {
            const hit = findColumnAt(x, y);
            if (!hit) return;
            const { col, drawX } = hit;
            try {
                if (col.property === '__selection') {
                    state.dotNet.invokeMethodAsync('OnToggleSelectAll');
                } else if (x > drawX + col.width - 45 && x < drawX + col.width - 25) {
                    state.dotNet.invokeMethodAsync('OnShowFilter', col.property, drawX, state.headerHeight);
                } else {
                    state.dotNet.invokeMethodAsync('OnHeaderClick', col.property);
                }
            } catch { }
            return;
        }

        const relativeY = y - state.headerHeight + state.scrollTop;
        const rowIndex = Math.floor(relativeY / state.rowHeight);
        if (rowIndex < 0 || rowIndex >= state.data.length) return;
        const row = state.data[rowIndex];
        try {
            if (isGroupRow(row)) {
                state.dotNet.invokeMethodAsync('ToggleGroupCollapsed', row._groupPath);
            } else {
                state.dotNet.invokeMethodAsync('OnRowClickInternal', rowIndex, e.shiftKey, e.ctrlKey || e.metaKey);
            }
        } catch { }
    };

    const onDblClick = (e) => {
        if (!state.dotNet) return;
        const { x, y } = getMousePos(e);
        if (y < state.headerHeight) return;
        const relativeY = y - state.headerHeight + state.scrollTop;
        const rowIndex = Math.floor(relativeY / state.rowHeight);
        if (rowIndex < 0 || rowIndex >= state.data.length) return;
        if (isGroupRow(state.data[rowIndex])) return;
        const hit = findColumnAt(x, 0);
        if (!hit) return;
        const { col, drawX } = hit;
        const cellY = (rowIndex * state.rowHeight) - state.scrollTop + state.headerHeight;
        try {
            state.dotNet.invokeMethodAsync('OnRowDoubleClickInternal',
                rowIndex, col.property, drawX, cellY, col.width, state.rowHeight);
        } catch { }
    };

    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('mouseleave', onMouseLeave);
    canvas.addEventListener('click', onClick);
    canvas.addEventListener('dblclick', onDblClick);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('resize', resize);

    state.listeners.push(
        () => canvas.removeEventListener('pointermove', onPointerMove),
        () => canvas.removeEventListener('pointerdown', onPointerDown),
        () => canvas.removeEventListener('mouseleave', onMouseLeave),
        () => canvas.removeEventListener('click', onClick),
        () => canvas.removeEventListener('dblclick', onDblClick),
        () => window.removeEventListener('pointerup', onPointerUp),
        () => window.removeEventListener('resize', resize)
    );
    state.recomputeColumnLayout = recomputeColumnLayout;

    canvas._sgGrid = state;

    if (typeof ResizeObserver !== 'undefined') {
        const ro = new ResizeObserver(() => resize());
        ro.observe(container);
        state.resizeObserver = ro;
        state.listeners.push(() => ro.disconnect());
    }

    resize();
}

export function dispose(canvas) {
    const state = canvas?._sgGrid;
    if (!state) return;
    if (state.listeners) {
        for (const off of state.listeners) {
            try { off(); } catch { }
        }
    }
    state.dotNet = null;
    state.data = [];
    canvas._sgGrid = null;
}

function isGroupRow(row) {
    return !Array.isArray(row) && row && row._isGroupRow;
}

function isSelectedRow(row) {
    if (Array.isArray(row)) return !!row[row.length - 1];
    return row ? !!row._selected : false;
}

function setRowSelected(row, value) {
    if (Array.isArray(row)) row[row.length - 1] = !!value;
    else if (row) row._selected = !!value;
}

function readCell(state, row, property) {
    if (Array.isArray(row)) {
        const i = state.columnIndex[property];
        if (i === undefined) return undefined;
        return row[i];
    }
    return row ? row[property] : undefined;
}

export function setData(canvas, data, columns, rowHeight, headerHeight, columnFilters, aggregates, groupBy, columnKeys, sortProperty, sortDescending) {
    const state = canvas?._sgGrid;
    if (!state) return;
    state.data = Array.isArray(data) ? data : [];
    state.columns = Array.isArray(columns) ? columns : [];
    state.rowHeight = rowHeight || 35;
    state.headerHeight = headerHeight || 40;
    state.columnFilters = columnFilters || {};
    state.aggregates = aggregates || {};
    state.groupBy = Array.isArray(groupBy) ? groupBy : [];
    state.hoveredRowIndex = -1;
    if (sortProperty !== undefined) {
        state.sortProperty = sortProperty || null;
        state.sortDescending = !!sortDescending;
    }

    state.columnKeys = Array.isArray(columnKeys) ? columnKeys : [];
    const idx = {};
    for (let i = 0; i < state.columnKeys.length; i++) idx[state.columnKeys[i]] = i;
    state.columnIndex = idx;

    state.recomputeColumnLayout();

    let dataRowCount = 0;
    let selectedDataRows = 0;
    for (const r of state.data) {
        if (r && !isGroupRow(r)) {
            dataRowCount++;
            if (isSelectedRow(r)) selectedDataRows++;
        }
    }
    state.allRowsSelected = dataRowCount > 0 && dataRowCount === selectedDataRows;

    const totalH = state.data.length * state.rowHeight;
    const scrollV = state.container.querySelector('.sg-canvas-grid-scroll-v');
    const scrollH = state.container.querySelector('.sg-canvas-grid-scroll-h');
    if (scrollV) {
        const inner = scrollV.querySelector('div');
        if (inner) inner.style.height = totalH + 'px';
    }
    if (scrollH) {
        const inner = scrollH.querySelector('div');
        if (inner) inner.style.width = state.totalWidth + 'px';
    }

    const viewH = Math.max(0, state.height - state.headerHeight - getFooterHeight(state));
    if (state.scrollTop > Math.max(0, totalH - viewH)) state.scrollTop = Math.max(0, totalH - viewH);
    if (state.scrollLeft > Math.max(0, state.totalWidth - (state.width - state.pinnedWidth))) {
        state.scrollLeft = Math.max(0, state.totalWidth - (state.width - state.pinnedWidth));
    }
    if (scrollV) scrollV.scrollTop = state.scrollTop;
    if (scrollH) scrollH.scrollLeft = state.scrollLeft;

    state.scheduleRender();
}

export function setSort(canvas, property, descending) {
    const state = canvas?._sgGrid;
    if (!state) return;
    state.sortProperty = property;
    state.sortDescending = descending;
    state.scheduleRender();
}

export function setSelectionAll(canvas, selected) {
    const state = canvas?._sgGrid;
    if (!state) return;
    const data = state.data;
    for (let i = 0; i < data.length; i++) {
        const r = data[i];
        if (r && !isGroupRow(r)) setRowSelected(r, selected);
    }
    state.allRowsSelected = !!selected;
    state.scheduleRender();
}

export function setSelectionAt(canvas, indices, selected) {
    const state = canvas?._sgGrid;
    if (!state) return;
    const data = state.data;
    for (const idx of indices) {
        const r = data[idx];
        if (r && !isGroupRow(r)) setRowSelected(r, selected);
    }
    let dataRows = 0, sel = 0;
    for (const r of data) {
        if (r && !isGroupRow(r)) {
            dataRows++;
            if (isSelectedRow(r)) sel++;
        }
    }
    state.allRowsSelected = dataRows > 0 && dataRows === sel;
    state.scheduleRender();
}

export function downloadCsv(fileName, content) {
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
        <body>${htmlContent}</body>
        </html>`;
    const blob = new Blob([template], { type: 'application/vnd.ms-excel' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

export function downloadImage(canvas, fileName) {
    if (!canvas) return;
    const url = canvas.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

function getFooterHeight(state) {
    return state.aggregates && Object.keys(state.aggregates).length > 0 ? state.rowHeight : 0;
}

function render(state) {
    const { ctx, width, height, data, rowHeight, headerHeight, scrollTop, scrollLeft,
            pinnedColumns, normalColumns, pinnedWidth } = state;
    if (!width || !height) return;

    ctx.clearRect(0, 0, width, height);

    const footerHeight = getFooterHeight(state);
    const contentHeight = Math.max(0, height - headerHeight - footerHeight);

    const startRow = Math.max(0, Math.floor(scrollTop / rowHeight));
    const endRow = Math.min(data.length, Math.ceil((scrollTop + contentHeight) / rowHeight));

    // Normal area
    if (width > pinnedWidth && contentHeight > 0) {
        ctx.save();
        ctx.beginPath();
        ctx.rect(pinnedWidth, headerHeight, width - pinnedWidth, contentHeight);
        ctx.clip();
        renderRowRange(state, normalColumns, pinnedWidth - scrollLeft, startRow, endRow, headerHeight, width, false);
        ctx.restore();
    }

    // Pinned area
    if (pinnedWidth > 0 && contentHeight > 0) {
        ctx.save();
        ctx.beginPath();
        ctx.rect(0, headerHeight, pinnedWidth, contentHeight);
        ctx.clip();
        renderRowRange(state, pinnedColumns, 0, startRow, endRow, headerHeight, pinnedWidth, true);
        ctx.restore();
    }

    drawGridLines(state, contentHeight);
    drawHeaders(state);
    drawAggregates(state);
}

function renderRowRange(state, cols, startX, startRow, endRow, regionTop, regionRight, isPinnedRegion) {
    if (cols.length === 0) return;
    const { ctx, data, rowHeight, hoveredRowIndex } = state;

    // Pre-compute visible columns and their x positions inside this region
    const visCols = [];
    {
        let cx = startX;
        for (const col of cols) {
            const colRight = cx + col.width;
            if (colRight > (isPinnedRegion ? 0 : state.pinnedWidth) && cx < regionRight) {
                visCols.push({ col, x: cx });
            }
            cx += col.width;
        }
    }
    if (visCols.length === 0) return;

    // Pass 1: backgrounds (one fillRect per row)
    for (let j = startRow; j < endRow; j++) {
        const row = data[j];
        if (!row) continue;
        const y = j * rowHeight - state.scrollTop + regionTop;

        if (isGroupRow(row)) {
            ctx.fillStyle = '#fafafa';
            ctx.fillRect(isPinnedRegion ? 0 : state.pinnedWidth, y, regionRight - (isPinnedRegion ? 0 : state.pinnedWidth), rowHeight);
            continue;
        }

        const sel = isSelectedRow(row);
        let bg = null;
        if (sel) bg = '#e6f7ff';
        else if (j === hoveredRowIndex) bg = '#f5f5f5';
        else if (j % 2 !== 0) bg = '#fafafa';

        if (bg) {
            ctx.fillStyle = bg;
            ctx.fillRect(isPinnedRegion ? 0 : state.pinnedWidth, y, regionRight - (isPinnedRegion ? 0 : state.pinnedWidth), rowHeight);
        }
    }

    // Pass 2: cell content (text + checkboxes). Set font/baseline once per region.
    ctx.font = FONT_CELL;
    ctx.textBaseline = 'middle';

    const halfRow = rowHeight / 2;
    for (let j = startRow; j < endRow; j++) {
        const row = data[j];
        if (!row || isGroupRow(row)) continue;

        const y = j * rowHeight - state.scrollTop + regionTop;
        const yMid = y + halfRow;

        const sel = isSelectedRow(row);
        ctx.fillStyle = sel ? '#1890ff' : '#333';
        let lastAlign = null;

        for (const v of visCols) {
            const col = v.col;
            const x = v.x;

            if (col.property === '__selection') {
                drawCheckbox(ctx, x + col.width / 2 - 7, y + halfRow - 7, 14, sel);
                ctx.fillStyle = sel ? '#1890ff' : '#333';
                continue;
            }

            const value = readCell(state, row, col.property);
            if (value == null || value === '') continue;
            const text = typeof value === 'string' ? value : String(value);

            const align = col.align || 'left';
            if (align !== lastAlign) {
                ctx.textAlign = align;
                lastAlign = align;
            }

            let textX;
            if (align === 'center') textX = x + col.width / 2;
            else if (align === 'right') textX = x + col.width - 10;
            else textX = x + 10;

            ctx.fillText(text, textX, yMid, col.width - 12);
        }
    }

    // Group rows pass (uses different font; these are sparse)
    let drewGroup = false;
    for (let j = startRow; j < endRow; j++) {
        const row = data[j];
        if (!row || !isGroupRow(row)) continue;
        const y = j * rowHeight - state.scrollTop + regionTop;
        drawGroupRowContent(state, row, y, isPinnedRegion);
        drewGroup = true;
    }
    if (drewGroup) {
        // restore for any subsequent draws that rely on cell font (none here, but safe)
        ctx.font = FONT_CELL;
    }
}

function drawGroupRowContent(state, row, y, isPinnedRegion) {
    const { ctx, rowHeight, columns, scrollLeft, pinnedWidth } = state;
    const regionLeft = isPinnedRegion ? 0 : pinnedWidth;
    const regionRight = isPinnedRegion ? pinnedWidth : state.width;

    // Bottom border
    ctx.strokeStyle = '#e8e8e8';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(regionLeft, y + rowHeight - 0.5);
    ctx.lineTo(regionRight, y + rowHeight - 0.5);
    ctx.stroke();

    // Label drawn in pinned region (or in normal if no pinned)
    if (isPinnedRegion || pinnedWidth === 0) {
        ctx.fillStyle = '#333';
        ctx.font = FONT_GROUP;
        ctx.textBaseline = 'middle';
        ctx.textAlign = 'left';
        const indent = (row._groupDepth || 0) * 20 + 10;
        const icon = row._isCollapsed ? '\u25B6' : '\u25BC';
        const yMid = y + rowHeight / 2;
        ctx.fillText(icon, regionLeft + indent, yMid);
        ctx.fillText(`${row._groupLabel} (${row._count})`, regionLeft + indent + 15, yMid);
    }

    // Aggregate values in columns
    ctx.fillStyle = '#666';
    ctx.font = FONT_GROUP_AGG;
    ctx.textBaseline = 'middle';
    let lastAlign = null;
    let cx = 0;
    const yMid = y + rowHeight / 2;
    for (const col of columns) {
        if (col.property === '__selection') { cx += col.width; continue; }
        if (state.groupBy && state.groupBy[row._groupDepth] === col.property) { cx += col.width; continue; }
        const val = readCell(state, row, col.property);
        if (val !== undefined && val !== null && val !== '') {
            const drawX = col.pinned ? cx : cx - scrollLeft;
            const colInPinned = !!col.pinned;
            if (colInPinned === isPinnedRegion && drawX + col.width > regionLeft && drawX < regionRight) {
                const align = col.align || 'left';
                if (align !== lastAlign) { ctx.textAlign = align; lastAlign = align; }
                let textX;
                if (align === 'center') textX = drawX + col.width / 2;
                else if (align === 'right') textX = drawX + col.width - 10;
                else textX = drawX + 10;
                ctx.fillText(String(val), textX, yMid, col.width - 12);
            }
        }
        cx += col.width;
    }
}

function drawAggregates(state) {
    const { ctx, rowHeight, width, height, aggregates, pinnedColumns, normalColumns, pinnedWidth, scrollLeft } = state;
    if (!aggregates || Object.keys(aggregates).length === 0) return;

    const y = height - rowHeight;
    ctx.fillStyle = '#f5f5f5';
    ctx.fillRect(0, y, width, rowHeight);
    ctx.strokeStyle = '#bfbfbf';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, y + 0.5);
    ctx.lineTo(width, y + 0.5);
    ctx.stroke();

    ctx.fillStyle = '#333';
    ctx.font = FONT_HEADER;
    ctx.textBaseline = 'middle';
    const yMid = y + rowHeight / 2;

    if (width > pinnedWidth) {
        ctx.save();
        ctx.beginPath();
        ctx.rect(pinnedWidth, y, width - pinnedWidth, rowHeight);
        ctx.clip();
        let cx = pinnedWidth - scrollLeft;
        let lastAlign = null;
        for (const col of normalColumns) {
            if (cx + col.width > pinnedWidth && cx < width) {
                const val = aggregates[col.property];
                if (val) {
                    const align = col.align || 'left';
                    if (align !== lastAlign) { ctx.textAlign = align; lastAlign = align; }
                    let textX;
                    if (align === 'center') textX = cx + col.width / 2;
                    else if (align === 'right') textX = cx + col.width - 10;
                    else textX = cx + 10;
                    ctx.fillText(val, textX, yMid, col.width - 12);
                }
                ctx.strokeStyle = '#e8e8e8';
                ctx.beginPath();
                ctx.moveTo(cx + col.width - 0.5, y);
                ctx.lineTo(cx + col.width - 0.5, y + rowHeight);
                ctx.stroke();
            }
            cx += col.width;
        }
        ctx.restore();
    }

    let cx = 0;
    let lastAlign = null;
    for (const col of pinnedColumns) {
        const val = aggregates[col.property];
        if (val) {
            const align = col.align || 'left';
            if (align !== lastAlign) { ctx.textAlign = align; lastAlign = align; }
            let textX;
            if (align === 'center') textX = cx + col.width / 2;
            else if (align === 'right') textX = cx + col.width - 10;
            else textX = cx + 10;
            ctx.fillText(val, textX, yMid, col.width - 12);
        }
        ctx.strokeStyle = '#e8e8e8';
        ctx.beginPath();
        ctx.moveTo(cx + col.width - 0.5, y);
        ctx.lineTo(cx + col.width - 0.5, y + rowHeight);
        ctx.stroke();
        cx += col.width;
    }
}

function drawHeaders(state) {
    const { ctx, headerHeight, scrollLeft, width, pinnedColumns, normalColumns, pinnedWidth } = state;

    ctx.fillStyle = '#f0f2f5';
    ctx.fillRect(0, 0, width, headerHeight);

    ctx.font = FONT_HEADER;
    ctx.textBaseline = 'middle';
    ctx.fillStyle = '#000';

    if (width > pinnedWidth) {
        ctx.save();
        ctx.beginPath();
        ctx.rect(pinnedWidth, 0, width - pinnedWidth, headerHeight);
        ctx.clip();
        let cx = pinnedWidth - scrollLeft;
        for (const col of normalColumns) {
            if (cx + col.width > pinnedWidth && cx < width) {
                drawHeaderCell(state, col, cx);
            }
            cx += col.width;
        }
        ctx.restore();
    }

    let cx = 0;
    for (const col of pinnedColumns) {
        drawHeaderCell(state, col, cx);
        cx += col.width;
    }

    ctx.strokeStyle = '#bfbfbf';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, headerHeight - 0.5);
    ctx.lineTo(width, headerHeight - 0.5);
    ctx.stroke();

    if (pinnedWidth > 0) {
        ctx.beginPath();
        ctx.moveTo(pinnedWidth - 0.5, 0);
        ctx.lineTo(pinnedWidth - 0.5, state.height);
        ctx.stroke();
    }
}

function drawHeaderCell(state, col, x) {
    const { ctx, headerHeight, sortProperty, sortDescending } = state;
    const yMid = headerHeight / 2;

    if (col.property === '__selection') {
        drawCheckbox(ctx, x + col.width / 2 - 7, yMid - 7, 14, state.allRowsSelected);
    } else {
        ctx.fillStyle = '#000';
        let textX;
        const align = col.align;
        if (align === 'center') { ctx.textAlign = 'center'; textX = x + col.width / 2; }
        else if (align === 'right') { ctx.textAlign = 'right'; textX = x + col.width - 45; }
        else { ctx.textAlign = 'left'; textX = x + 10; }

        ctx.fillText(col.title || '', textX, yMid, Math.max(0, col.width - 50));

        if (col.filterable !== false) {
            const filterX = x + col.width - 40;
            ctx.fillStyle = state.columnFilters?.[col.property] === 'active' ? '#1890ff' : '#999';
            ctx.beginPath();
            ctx.moveTo(filterX - 4, yMid - 4);
            ctx.lineTo(filterX + 4, yMid - 4);
            ctx.lineTo(filterX + 1, yMid);
            ctx.lineTo(filterX + 1, yMid + 4);
            ctx.lineTo(filterX - 1, yMid + 4);
            ctx.lineTo(filterX - 1, yMid);
            ctx.closePath();
            ctx.fill();
        }

        if (sortProperty === col.property) {
            const sortX = x + col.width - 20;
            ctx.fillStyle = '#1890ff';
            ctx.beginPath();
            if (sortDescending) {
                ctx.moveTo(sortX - 4, yMid - 2);
                ctx.lineTo(sortX + 4, yMid - 2);
                ctx.lineTo(sortX, yMid + 4);
            } else {
                ctx.moveTo(sortX - 4, yMid + 2);
                ctx.lineTo(sortX + 4, yMid + 2);
                ctx.lineTo(sortX, yMid - 4);
            }
            ctx.closePath();
            ctx.fill();
        }
    }

    ctx.strokeStyle = '#bfbfbf';
    ctx.beginPath();
    ctx.moveTo(x + col.width - 0.5, 0);
    ctx.lineTo(x + col.width - 0.5, headerHeight);
    ctx.stroke();
}

function drawCheckbox(ctx, x, y, size, checked) {
    ctx.lineWidth = 1;
    if (checked) {
        ctx.fillStyle = '#1890ff';
        ctx.strokeStyle = '#1890ff';
    } else {
        ctx.fillStyle = '#fff';
        ctx.strokeStyle = '#d9d9d9';
    }
    ctx.beginPath();
    if (ctx.roundRect) ctx.roundRect(x, y, size, size, 2);
    else ctx.rect(x, y, size, size);
    ctx.fill();
    ctx.stroke();

    if (checked) {
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(x + 3, y + size / 2);
        ctx.lineTo(x + size / 2 - 1, y + size - 4);
        ctx.lineTo(x + size - 3, y + 4);
        ctx.stroke();
    }
}

function drawGridLines(state, contentHeight) {
    const { ctx, width, columns, rowHeight, headerHeight, scrollTop, scrollLeft, data, pinnedWidth } = state;
    ctx.strokeStyle = '#e8e8e8';
    ctx.lineWidth = 1;

    // Vertical column lines: batched into a single path
    ctx.beginPath();
    let cx = 0;
    for (const col of columns) {
        const drawX = col.pinned ? cx : cx - scrollLeft;
        if (col.pinned || (drawX + col.width > pinnedWidth && drawX < width)) {
            const lx = drawX + col.width - 0.5;
            ctx.moveTo(lx, headerHeight);
            ctx.lineTo(lx, headerHeight + contentHeight);
        }
        cx += col.width;
    }
    ctx.stroke();

    // Horizontal row lines: batched
    const startRow = Math.max(0, Math.floor(scrollTop / rowHeight));
    const endRow = Math.min(data.length, Math.ceil((scrollTop + contentHeight) / rowHeight));
    ctx.beginPath();
    for (let j = startRow; j <= endRow; j++) {
        const y = j * rowHeight - scrollTop + headerHeight;
        if (y > headerHeight && y < headerHeight + contentHeight) {
            ctx.moveTo(0, y - 0.5);
            ctx.lineTo(width, y - 0.5);
        }
    }
    ctx.stroke();
}
