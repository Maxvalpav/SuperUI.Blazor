// ─── SuperUI Image Editor JS Module ─────────────────────────────────────────

export function init(mainCanvas, overlayCanvas, dotNetRef, options) {
    const ctx = mainCanvas.getContext('2d');
    const ovCtx = overlayCanvas.getContext('2d');
    if (!ctx || !ovCtx) return;

    const opt = Object.assign({
        penColor: '#1e293b', penWidth: 2, bgColor: '#ffffff'
    }, options || {});

    // ── State ──
    let currentState = null;
    let imageData = null;            // original loaded pixels
    let hasImage = false;
    let resizeObserver = null;
    let activeTool = 'select';

    // Drawing state
    let isDrawing = false;
    let drawStroke = [];

    // Crop state
    let isCropping = false;
    let cropStart = null;
    let cropRect = null;             // { x, y, w, h }
    let cropDragHandle = null;       // 'nw','ne','sw','se','move' or null
    let cropDragOffset = null;

    // Undo/Redo
    const undoStack = [];
    const redoStack = [];
    const maxUndo = 50;

    function saveState() {
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return;
        undoStack.push(ctx.getImageData(0, 0, w, h));
        if (undoStack.length > maxUndo) undoStack.shift();
        redoStack.length = 0;
        notifyUndoRedo();
    }

    function notifyUndoRedo() {
        try { dotNetRef.invokeMethodAsync('OnUndoRedoChangedJs',
            undoStack.length > 0, redoStack.length > 0); } catch {}
    }

    // ── Resize ──
    function fitCanvases() {
        const parent = mainCanvas.parentElement;
        if (!parent) return;
        const rect = parent.getBoundingClientRect();
        const w = Math.round(rect.width), h = Math.round(rect.height);
        if (!w || !h) return;
        if (w === mainCanvas.width && h === mainCanvas.height) return;
        mainCanvas.width = w; mainCanvas.height = h;
        overlayCanvas.width = w; overlayCanvas.height = h;
        if (currentState) ctx.putImageData(currentState, 0, 0);
    }

    if (window.ResizeObserver) {
        resizeObserver = new ResizeObserver(() => fitCanvases());
        resizeObserver.observe(mainCanvas.parentElement || mainCanvas);
    }
    fitCanvases();

    function toCanvasCoords(clientX, clientY) {
        const r = overlayCanvas.getBoundingClientRect();
        return { x: clientX - r.left, y: clientY - r.top };
    }

    // ── Crop rendering on overlay ──
    function clearOverlay() {
        ovCtx.clearRect(0, 0, overlayCanvas.width, overlayCanvas.height);
    }

    function drawCropRect() {
        if (!cropRect) return;
        const { x, y, w, h } = cropRect;
        const cw = overlayCanvas.width, ch = overlayCanvas.height;

        clearOverlay();

        // Dimmed overlay outside selection
        ovCtx.fillStyle = 'rgba(0,0,0,0.45)';
        ovCtx.fillRect(0, 0, cw, y);
        ovCtx.fillRect(0, y + h, cw, ch - y - h);
        ovCtx.fillRect(0, y, x, h);
        ovCtx.fillRect(x + w, y, cw - x - w, h);

        // Selection border
        ovCtx.strokeStyle = '#fff';
        ovCtx.lineWidth = 1.5;
        ovCtx.strokeRect(x, y, w, h);

        // Corner handles
        const hs = 8, hh = hs / 2;
        ovCtx.fillStyle = '#fff';
        ovCtx.strokeStyle = '#3b82f6';
        ovCtx.lineWidth = 2;
        for (const [cx, cy] of [[x, y], [x + w, y], [x, y + h], [x + w, y + h]]) {
            ovCtx.fillRect(cx - hh, cy - hh, hs, hs);
            ovCtx.strokeRect(cx - hh, cy - hh, hs, hs);
        }

        // Rule-of-thirds grid
        ovCtx.strokeStyle = 'rgba(255,255,255,0.3)';
        ovCtx.lineWidth = 0.5;
        ovCtx.setLineDash([4, 4]);
        const x1 = x + w / 3, x2 = x + 2 * w / 3;
        const y1 = y + h / 3, y2 = y + 2 * h / 3;
        ovCtx.beginPath();
        ovCtx.moveTo(x1, y); ovCtx.lineTo(x1, y + h);
        ovCtx.moveTo(x2, y); ovCtx.lineTo(x2, y + h);
        ovCtx.moveTo(x, y1); ovCtx.lineTo(x + w, y1);
        ovCtx.moveTo(x, y2); ovCtx.lineTo(x + w, y2);
        ovCtx.stroke();
        ovCtx.setLineDash([]);
    }

    function getCropHandle(pos) {
        if (!cropRect) return null;
        const { x, y, w, h } = cropRect;
        const hs = 10; // handle hit area
        const corners = {
            nw: { x, y }, ne: { x: x + w, y },
            sw: { x, y: y + h }, se: { x: x + w, y: y + h }
        };
        for (const [name, p] of Object.entries(corners)) {
            if (Math.abs(pos.x - p.x) <= hs && Math.abs(pos.y - p.y) <= hs)
                return name;
        }
        // Inside rectangle → move
        if (pos.x >= x && pos.x <= x + w && pos.y >= y && pos.y <= y + h)
            return 'move';
        return null;
    }

    function clampRect(rect) {
        const cw = overlayCanvas.width, ch = overlayCanvas.height;
        let x = Math.max(0, Math.min(rect.x, cw - 10));
        let y = Math.max(0, Math.min(rect.y, ch - 10));
        let w = Math.max(10, Math.min(rect.w, cw - x));
        let h = Math.max(10, Math.min(rect.h, ch - y));
        if (x + w > cw) w = cw - x;
        if (y + h > ch) h = ch - y;
        return { x, y, w, h };
    }

    // ── Tool switching ──
    function setActiveTool(tool) {
        activeTool = tool;
        if (tool !== 'crop') {
            cropRect = null;
            isCropping = false;
            cropStart = null;
            clearOverlay();
            try { dotNetRef.invokeMethodAsync('OnCropActiveChangedJs', false); } catch {}
        }
        overlayCanvas.style.cursor = tool === 'draw' ? 'crosshair'
            : tool === 'crop' ? 'crosshair' : 'default';
        overlayCanvas.style.pointerEvents = (tool === 'draw' || tool === 'crop') ? 'auto' : 'none';
        if (tool === 'crop') {
            overlayCanvas.style.cursor = 'crosshair';
        }
    }

    // ── Mouse event handlers ──
    function onMouseDown(e) {
        const pos = toCanvasCoords(e.clientX, e.clientY);

        if (activeTool === 'draw') {
            isDrawing = true;
            drawStroke = [pos];
            clearOverlay();
            ovCtx.beginPath();
            ovCtx.moveTo(pos.x, pos.y);
            ovCtx.strokeStyle = opt.penColor;
            ovCtx.lineWidth = opt.penWidth;
            ovCtx.lineCap = 'round';
            ovCtx.lineJoin = 'round';
            return;
        }

        if (activeTool === 'crop') {
            if (cropRect) {
                const handle = getCropHandle(pos);
                if (handle) {
                    cropDragHandle = handle;
                    if (handle === 'move') {
                        cropDragOffset = { x: pos.x - cropRect.x, y: pos.y - cropRect.y };
                    } else {
                        const c = { nw: 'se', ne: 'sw', sw: 'ne', se: 'nw' }[handle];
                        cropDragOffset = { anchor: c, startX: cropRect.x, startY: cropRect.y,
                            startW: cropRect.w, startH: cropRect.h, mx: pos.x, my: pos.y };
                    }
                    return;
                }
            }
            // Start new crop selection
            isCropping = true;
            cropStart = pos;
            cropRect = null;
        }
    }

    function onMouseMove(e) {
        const pos = toCanvasCoords(e.clientX, e.clientY);

        if (isDrawing && activeTool === 'draw') {
            drawStroke.push(pos);
            ovCtx.lineTo(pos.x, pos.y);
            ovCtx.stroke();
            return;
        }

        if (activeTool === 'crop') {
            if (cropDragHandle) {
                if (cropDragHandle === 'move') {
                    const dx = pos.x - cropDragOffset.x;
                    const dy = pos.y - cropDragOffset.y;
                    cropRect = clampRect({ x: dx, y: dy, w: cropRect.w, h: cropRect.h });
                } else {
                    // Resize from corner handle
                    const anc = cropDragOffset.anchor;
                    let { x, y, w, h } = cropDragOffset;
                    const dx = pos.x - cropDragOffset.mx;
                    const dy = pos.y - cropDragOffset.my;
                    if (anc === 'se') { w += dx; h += dy; }
                    else if (anc === 'sw') { x += dx; w -= dx; h += dy; }
                    else if (anc === 'ne') { y += dy; w += dx; h -= dy; }
                    else if (anc === 'nw') { x += dx; y += dy; w -= dx; h -= dy; }
                    cropRect = clampRect({ x, y, w, h });
                }
                drawCropRect();
                return;
            }

            if (isCropping && cropStart) {
                const x = Math.min(cropStart.x, pos.x);
                const y = Math.min(cropStart.y, pos.y);
                const w = Math.abs(pos.x - cropStart.x);
                const h = Math.abs(pos.y - cropStart.y);
                cropRect = clampRect({ x, y, w, h });
                drawCropRect();
                return;
            }

            // Update cursor for handles
            const handle = getCropHandle(pos);
            if (handle === 'move') overlayCanvas.style.cursor = 'move';
            else if (handle) overlayCanvas.style.cursor = handle === 'nw' || handle === 'se' ? 'nwse-resize'
                : 'nesw-resize';
            else overlayCanvas.style.cursor = 'crosshair';
        }
    }

    function onMouseUp() {
        if (isDrawing) {
            isDrawing = false;
            // Flatten drawing onto main canvas
            const w = mainCanvas.width, h = mainCanvas.height;
            if (w && h) {
                saveState();
                ctx.drawImage(overlayCanvas, 0, 0);
                currentState = ctx.getImageData(0, 0, w, h);
            }
            clearOverlay();
            drawStroke = [];
            return;
        }

        if (activeTool === 'crop') {
            if (isCropping) {
                isCropping = false;
                cropStart = null;
                if (cropRect) {
                    try { dotNetRef.invokeMethodAsync('OnCropActiveChangedJs', true); } catch {}
                }
            }
            cropDragHandle = null;
            cropDragOffset = null;
        }
    }

    overlayCanvas.addEventListener('mousedown', onMouseDown);
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);

    // Touch
    function onTouchStart(e) { e.preventDefault(); const t = e.touches[0]; onMouseDown({ clientX: t.clientX, clientY: t.clientY }); }
    function onTouchMove(e) { e.preventDefault(); const t = e.touches[0]; onMouseMove({ clientX: t.clientX, clientY: t.clientY }); }
    overlayCanvas.addEventListener('touchstart', onTouchStart, { passive: false });
    overlayCanvas.addEventListener('touchmove', onTouchMove, { passive: false });
    overlayCanvas.addEventListener('touchend', onMouseUp);
    overlayCanvas.addEventListener('touchcancel', onMouseUp);

    // ── Image loading ──
    mainCanvas._loadImage = (src) => {
        return new Promise((resolve) => {
            const img = new Image();
            img.crossOrigin = 'anonymous';
            img.onload = () => {
                fitCanvases();
                hasImage = true;
                const w = mainCanvas.width, h = mainCanvas.height;
                ctx.clearRect(0, 0, w, h);
                ctx.drawImage(img, 0, 0, w, h);
                currentState = ctx.getImageData(0, 0, w, h);
                imageData = ctx.getImageData(0, 0, w, h);
                undoStack.length = 0; redoStack.length = 0;
                notifyUndoRedo();
                try { dotNetRef.invokeMethodAsync('OnImageLoadedJs', img.naturalWidth, img.naturalHeight); } catch {}
                resolve(true);
            };
            img.onerror = () => resolve(false);
            img.src = src;
        });
    };

    mainCanvas._getDataUrl = (fmt, q) => mainCanvas.toDataURL(fmt || 'image/png', q || 0.92);

    // ── Filter ──
    mainCanvas._applyFilter = (filterValue) => {
        if (!currentState) return;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return;
        saveState();
        const imgData = ctx.getImageData(0, 0, w, h);
        const tmp = document.createElement('canvas');
        tmp.width = w; tmp.height = h;
        const tc = tmp.getContext('2d');
        tc.putImageData(imgData, 0, 0);
        ctx.clearRect(0, 0, w, h);
        ctx.filter = filterValue || 'none';
        ctx.drawImage(tmp, 0, 0);
        ctx.filter = 'none';
        currentState = ctx.getImageData(0, 0, w, h);
    };

    // ── Rotate ──
    mainCanvas._rotate = (degrees) => {
        if (!currentState) return;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return;
        saveState();

        if (degrees === 180) {
            ctx.clearRect(0, 0, w, h);
            ctx.save();
            ctx.translate(w / 2, h / 2);
            ctx.rotate(Math.PI);
            ctx.drawImage(mainCanvas, -w / 2, -h / 2);
            ctx.restore();
            currentState = ctx.getImageData(0, 0, w, h);
            return;
        }

        const tmp = document.createElement('canvas');
        tmp.width = h; tmp.height = w;
        const tc = tmp.getContext('2d');
        tc.save();
        tc.translate(h / 2, w / 2);
        tc.rotate(degrees * Math.PI / 180);
        tc.drawImage(mainCanvas, -w / 2, -h / 2);
        tc.restore();

        mainCanvas.width = h; mainCanvas.height = w;
        overlayCanvas.width = h; overlayCanvas.height = w;
        ctx.drawImage(tmp, 0, 0);
        currentState = ctx.getImageData(0, 0, h, w);
    };

    // ── Flip ──
    mainCanvas._flip = (horizontal, vertical) => {
        if (!currentState) return;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return;
        saveState();
        ctx.save();
        ctx.translate(horizontal ? w : 0, vertical ? h : 0);
        ctx.scale(horizontal ? -1 : 1, vertical ? -1 : 1);
        ctx.drawImage(mainCanvas, 0, 0);
        ctx.restore();
        currentState = ctx.getImageData(0, 0, w, h);
    };

    // ── Crop ──
    mainCanvas._getCropRect = () => cropRect ? { ...cropRect } : null;
    mainCanvas._clearCrop = () => { cropRect = null; clearOverlay(); };
    mainCanvas._applyCrop = () => {
        if (!cropRect || !currentState) return false;
        const r = cropRect;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h || r.w <= 1 || r.h <= 1) return false;
        saveState();
        const imgData = ctx.getImageData(r.x, r.y, r.w, r.h);
        mainCanvas.width = r.w; mainCanvas.height = r.h;
        overlayCanvas.width = r.w; overlayCanvas.height = r.h;
        // Wait for resize then put data
        requestAnimationFrame(() => {
            ctx.putImageData(imgData, 0, 0);
            currentState = ctx.getImageData(0, 0, r.w, r.h);
        });
        cropRect = null;
        clearOverlay();
        return true;
    };

    // ── Resize canvas ──
    mainCanvas._resizeCanvas = (nw, nh) => {
        if (!currentState) return;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return;
        saveState();
        const tmp = document.createElement('canvas');
        tmp.width = w; tmp.height = h;
        tmp.getContext('2d').putImageData(currentState, 0, 0);
        mainCanvas.width = nw; mainCanvas.height = nh;
        overlayCanvas.width = nw; overlayCanvas.height = nh;
        ctx.drawImage(tmp, 0, 0, nw, nh);
        currentState = ctx.getImageData(0, 0, nw, nh);
    };

    // ── Public methods ──
    mainCanvas._setTool = (tool) => setActiveTool(tool);
    mainCanvas._setPenColor = (c) => { opt.penColor = c; };
    mainCanvas._setPenWidth = (w) => { opt.penWidth = w; };

    mainCanvas._undo = () => {
        if (!undoStack.length) return false;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return false;
        redoStack.push(ctx.getImageData(0, 0, w, h));
        const state = undoStack.pop();
        ctx.putImageData(state, 0, 0);
        currentState = ctx.getImageData(0, 0, w, h);
        notifyUndoRedo();
        return true;
    };

    mainCanvas._redo = () => {
        if (!redoStack.length) return false;
        const w = mainCanvas.width, h = mainCanvas.height;
        if (!w || !h) return false;
        undoStack.push(ctx.getImageData(0, 0, w, h));
        const state = redoStack.pop();
        ctx.putImageData(state, 0, 0);
        currentState = ctx.getImageData(0, 0, w, h);
        notifyUndoRedo();
        return true;
    };

    mainCanvas._reset = () => {
        if (!imageData) return;
        const w = imageData.width, h = imageData.height;
        mainCanvas.width = w; mainCanvas.height = w; // temp, resizeCanvas will fix
        overlayCanvas.width = w; overlayCanvas.height = w;
        fitCanvases();
        // Put original back, scaled to current canvas
        const cw = mainCanvas.width, ch = mainCanvas.height;
        ctx.clearRect(0, 0, cw, ch);
        const tmp = document.createElement('canvas');
        tmp.width = imageData.width; tmp.height = imageData.height;
        tmp.getContext('2d').putImageData(imageData, 0, 0);
        ctx.drawImage(tmp, 0, 0, cw, ch);
        currentState = ctx.getImageData(0, 0, cw, ch);
        undoStack.length = 0; redoStack.length = 0;
        notifyUndoRedo();
        clearOverlay();
    };

    mainCanvas._download = (filename, fmt, q) => {
        const a = document.createElement('a');
        a.download = filename || 'image.png';
        a.href = mainCanvas.toDataURL(fmt || 'image/png', q || 0.92);
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    };

    // ── Keyboard shortcuts ──
    function onKeyDown(e) {
        if (e.ctrlKey && e.key === 'z') {
            e.preventDefault();
            if (e.shiftKey) mainCanvas._redo();
            else mainCanvas._undo();
        }
        if (e.ctrlKey && e.key === 'y') {
            e.preventDefault();
            mainCanvas._redo();
        }
        if (e.key === 'Escape' && activeTool === 'crop') {
            cropRect = null; clearOverlay();
            try { dotNetRef.invokeMethodAsync('OnCropActiveChangedJs', false); } catch {}
        }
        if (e.key === 'Enter' && activeTool === 'crop' && cropRect) {
            mainCanvas._applyCrop();
            try { dotNetRef.invokeMethodAsync('OnCropActiveChangedJs', false); } catch {}
        }
    }
    document.addEventListener('keydown', onKeyDown);
    mainCanvas._keyDownHandler = onKeyDown;

    // ── Dispose ──
    mainCanvas._dispose = () => {
        if (resizeObserver) { try { resizeObserver.disconnect(); } catch {} resizeObserver = null; }
        if (mainCanvas._keyDownHandler) {
            try { document.removeEventListener('keydown', mainCanvas._keyDownHandler); } catch {}
            mainCanvas._keyDownHandler = null;
        }
        for (const f of ['_loadImage','_getDataUrl','_applyFilter','_rotate','_flip',
            '_crop','_resizeCanvas','_setTool','_setPenColor','_setPenWidth',
            '_undo','_redo','_reset','_download','_dispose',
            '_getCropRect','_clearCrop','_applyCrop','_keyDownHandler']) {
            try { mainCanvas[f] = null; } catch {}
        }
    };
}

// ── Exported wrappers ───────────────────────────────────────────────────────

export function loadImage(canvas, src) { return canvas?._loadImage ? canvas._loadImage(src) : Promise.resolve(false); }
export function getDataUrl(canvas, fmt, q) { return canvas?._getDataUrl ? canvas._getDataUrl(fmt, q) : ''; }
export function applyFilter(canvas, v) { if (canvas?._applyFilter) canvas._applyFilter(v); }
export function rotate(canvas, d) { if (canvas?._rotate) canvas._rotate(d); }
export function flip(canvas, h, v) { if (canvas?._flip) canvas._flip(h, v); }
export function getCropRect(canvas) { return canvas?._getCropRect ? canvas._getCropRect() : null; }
export function clearCrop(canvas) { if (canvas?._clearCrop) canvas._clearCrop(); }
export function applyCrop(canvas) { return canvas?._applyCrop ? canvas._applyCrop() : false; }
export function resizeCanvas(canvas, w, h) { if (canvas?._resizeCanvas) canvas._resizeCanvas(w, h); }
export function setTool(canvas, t) { if (canvas?._setTool) canvas._setTool(t); }
export function setPenColor(canvas, c) { if (canvas?._setPenColor) canvas._setPenColor(c); }
export function setPenWidth(canvas, w) { if (canvas?._setPenWidth) canvas._setPenWidth(w); }
export function undo(canvas) { return canvas?._undo ? canvas._undo() : false; }
export function redo(canvas) { return canvas?._redo ? canvas._redo() : false; }
export function reset(canvas) { if (canvas?._reset) canvas._reset(); }
export function download(canvas, fn, fmt, q) { if (canvas?._download) canvas._download(fn, fmt, q); }
export function dispose(canvas) { if (canvas?._dispose) canvas._dispose(); }
