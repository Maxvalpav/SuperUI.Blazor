export function init(canvas, dotNetRef, penColor, penWidth, bgColor, readOnly,
    showGuideLine, guideLineColor, guideLineText, guideLineStyle, initialsOnly, label) {

    const ctx = canvas.getContext('2d');
    let isDrawing = false;
    let hasDrawn = false;
    const undoStack = [];
    const maxUndo = 500;
    let redoStack = [];
    let resizeObserver = null;
    let strokes = [];
    let currentStroke = null;
    let guideY = null;
    let guideTextWidth = 0;
    let labelText = label || '';

    function saveState() {
        undoStack.push(ctx.getImageData(0, 0, canvas.width, canvas.height));
        if (undoStack.length > maxUndo) undoStack.shift();
        redoStack = [];
    }

    function drawGuideLine() {
        if (!showGuideLine || guideY === null) return;
        ctx.save();
        ctx.strokeStyle = guideLineColor;
        ctx.lineWidth = 1;
        ctx.setLineDash(guideLineStyle === 'Dotted' ? [2, 4] : guideLineStyle === 'Dashed' ? [8, 6] : []);
        ctx.beginPath();
        ctx.moveTo(0, guideY);
        ctx.lineTo(canvas.width, guideY);
        ctx.stroke();
        ctx.setLineDash([]);
        if (guideLineText) {
            ctx.fillStyle = guideLineColor;
            ctx.font = '11px sans-serif';
            ctx.textAlign = 'right';
            ctx.textBaseline = 'bottom';
            ctx.fillText(guideLineText, canvas.width - 8, guideY - 4);
        }
        ctx.restore();
    }

    function recalcGuideY() {
        guideY = initialsOnly ? canvas.height * 0.65 : canvas.height * 0.78;
    }

    function applyBg(color) {
        if (!color || color === 'transparent') return;
        ctx.fillStyle = color;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
    }

    function fullRender() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        applyBg(bgColor);
        for (const stroke of strokes) {
            if (stroke.points.length < 2) continue;
            ctx.beginPath();
            ctx.strokeStyle = stroke.color;
            ctx.lineWidth = stroke.width;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.moveTo(stroke.points[0].x, stroke.points[0].y);
            for (let i = 1; i < stroke.points.length; i++) {
                ctx.lineTo(stroke.points[i].x, stroke.points[i].y);
            }
            ctx.stroke();
        }
        drawGuideLine();
    }

    function resizeCanvas() {
        const rect = canvas.getBoundingClientRect();
        const w = Math.round(rect.width);
        const h = Math.round(rect.height);
        if (w === canvas.width && h === canvas.height) return;
        canvas.width = w || 1;
        canvas.height = h || 1;
        recalcGuideY();
        fullRender();
        ctx.strokeStyle = penColor;
        ctx.lineWidth = penWidth;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
    }

    resizeCanvas();
    drawGuideLine();

    function getPos(e) {
        const r = canvas.getBoundingClientRect();
        if (e.touches) {
            const t = e.touches[0];
            return { x: t.clientX - r.left, y: t.clientY - r.top, pressure: t.force || 0.5 };
        }
        if (e.offsetX !== undefined) {
            return { x: e.offsetX, y: e.offsetY, pressure: 0.5 };
        }
        return { x: e.clientX - r.left, y: e.clientY - r.top, pressure: 0.5 };
    }

    function start(e) {
        if (readOnly) return;
        isDrawing = true;
        saveState();
        const pos = getPos(e);
        currentStroke = {
            color: penColor,
            width: penWidth,
            points: [{ x: pos.x, y: pos.y, pressure: pos.pressure }]
        };
        const w = pos.pressure > 0.5 ? penWidth * Math.min(pos.pressure * 2, 2) : penWidth;
        ctx.beginPath();
        ctx.moveTo(pos.x, pos.y);
        ctx.lineWidth = w;
        ctx.strokeStyle = penColor;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        hasDrawn = true;
        try { dotNetRef.invokeMethodAsync('OnDrawStartJs', pos.x, pos.y); } catch {}
    }

    function draw(e) {
        if (!isDrawing || readOnly) return;
        const pos = getPos(e);
        if (currentStroke) currentStroke.points.push({ x: pos.x, y: pos.y, pressure: pos.pressure });
        const w = pos.pressure > 0.5 ? penWidth * Math.min(pos.pressure * 2, 2) : penWidth;
        if (Math.abs(ctx.lineWidth - w) > 0.5) {
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(pos.x, pos.y);
            ctx.lineWidth = w;
        }
        ctx.lineTo(pos.x, pos.y);
        ctx.stroke();
    }

    function stop() {
        if (!isDrawing) return;
        isDrawing = false;
        if (currentStroke) {
            strokes.push(currentStroke);
            currentStroke = null;
        }
        try { dotNetRef.invokeMethodAsync('OnDrawEndJs'); } catch {}
        try { dotNetRef.invokeMethodAsync('OnChangeJs', strokes.length > 0, strokes.length); } catch {}
    }

    const onTouchStart = (e) => { e.preventDefault(); start(e); };
    const onTouchMove = (e) => { e.preventDefault(); draw(e); };

    canvas.addEventListener('mousedown', start);
    canvas.addEventListener('mousemove', draw);
    window.addEventListener('mouseup', stop);
    canvas.addEventListener('mouseleave', stop);
    canvas.addEventListener('touchstart', onTouchStart, { passive: false });
    canvas.addEventListener('touchmove', onTouchMove, { passive: false });
    canvas.addEventListener('touchend', stop);
    canvas.addEventListener('touchcancel', stop);

    if (window.ResizeObserver) {
        resizeObserver = new ResizeObserver(() => resizeCanvas());
        resizeObserver.observe(canvas.parentElement || canvas);
    }

    canvas._handlers = { start, draw, stop, onTouchStart, onTouchMove };

    function findBoundingBox() {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;
        const bgR = 255, bgG = 255, bgB = 255;
        let minX = canvas.width, minY = canvas.height, maxX = 0, maxY = 0;
        let found = false;
        for (let y = 0; y < canvas.height; y++) {
            for (let x = 0; x < canvas.width; x++) {
                const i = (y * canvas.width + x) * 4;
                const dr = Math.abs(data[i] - bgR);
                const dg = Math.abs(data[i + 1] - bgG);
                const db = Math.abs(data[i + 2] - bgB);
                if (dr > 10 || dg > 10 || db > 10) {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    found = true;
                }
            }
        }
        if (!found) return null;
        const pad = 4;
        return {
            x: Math.max(0, minX - pad),
            y: Math.max(0, minY - pad),
            w: Math.min(canvas.width - minX + pad, maxX - minX + pad * 2),
            h: Math.min(canvas.height - minY + pad, maxY - minY + pad * 2)
        };
    }

    canvas._clear = () => {
        strokes = [];
        currentStroke = null;
        hasDrawn = false;
        undoStack.length = 0;
        redoStack.length = 0;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        applyBg(bgColor);
        drawGuideLine();
    };

    canvas._getDataUrl = (format, quality) => canvas.toDataURL(format || 'image/png', quality);
    canvas._getTrimmedDataUrl = (format, quality) => {
        const bb = findBoundingBox();
        if (!bb) return canvas.toDataURL(format || 'image/png', quality);
        const temp = document.createElement('canvas');
        temp.width = bb.w;
        temp.height = bb.h;
        const tctx = temp.getContext('2d');
        tctx.fillStyle = bgColor;
        tctx.fillRect(0, 0, bb.w, bb.h);
        tctx.drawImage(canvas, bb.x, bb.y, bb.w, bb.h, 0, 0, bb.w, bb.h);
        return temp.toDataURL(format || 'image/png', quality);
    };

    canvas._isEmpty = () => strokes.length === 0;
    canvas._getStrokeCount = () => strokes.length;

    canvas._undo = () => {
        if (strokes.length === 0) return false;
        strokes.pop();
        redoStack.push(ctx.getImageData(0, 0, canvas.width, canvas.height));
        fullRender();
        hasDrawn = strokes.length > 0;
        try { dotNetRef.invokeMethodAsync('OnChangeJs', strokes.length > 0, strokes.length); } catch {}
        return true;
    };

    canvas._redo = () => {
        if (redoStack.length === 0) return false;
        saveState();
        const state = redoStack.pop();
        ctx.putImageData(state, 0, 0);
        strokes.push({ restored: true });
        hasDrawn = true;
        try { dotNetRef.invokeMethodAsync('OnChangeJs', true, strokes.length); } catch {}
        return true;
    };

    canvas._setPenColor = (c) => { penColor = c; };
    canvas._setPenWidth = (w) => { penWidth = w; };
    canvas._setReadOnly = (r) => { readOnly = r; };
    canvas._setBgColor = (c) => { bgColor = c; applyBg(c); drawGuideLine(); };
    canvas._resize = () => resizeCanvas();

    canvas._loadImage = (dataUrl) => {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                strokes = [];
                currentStroke = null;
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                applyBg(bgColor);
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                hasDrawn = true;
                drawGuideLine();
                resolve(true);
            };
            img.onerror = () => resolve(false);
            img.src = dataUrl;
        });
    };

    canvas._getStrokes = () => strokes.length;

    canvas._replay = (strokeIndex) => {
        return new Promise((resolve) => {
            if (strokes.length === 0) { resolve(false); return; }
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            applyBg(bgColor);
            const target = strokeIndex >= 0 ? Math.min(strokeIndex + 1, strokes.length) : strokes.length;
            let current = 0;
            function drawNextStroke() {
                if (current >= target) { drawGuideLine(); resolve(true); return; }
                const stroke = strokes[current];
                if (!stroke.points || stroke.points.length < 2) { current++; drawNextStroke(); return; }
                const pts = stroke.points;
                let i = 0;
                function drawPoint() {
                    if (i >= pts.length) { ctx.stroke(); current++; drawNextStroke(); return; }
                    const p = pts[i];
                    if (i === 0) {
                        ctx.beginPath();
                        ctx.strokeStyle = stroke.color;
                        ctx.lineWidth = stroke.width;
                        ctx.lineCap = 'round';
                        ctx.lineJoin = 'round';
                        ctx.moveTo(p.x, p.y);
                    } else {
                        ctx.lineTo(p.x, p.y);
                        ctx.stroke();
                    }
                    i++;
                    requestAnimationFrame(drawPoint);
                }
                drawPoint();
            }
            drawNextStroke();
        });
    };

    canvas._download = (filename, format, quality) => {
        const link = document.createElement('a');
        link.download = filename || 'signature.png';
        link.href = canvas.toDataURL(format || 'image/png', quality);
        link.click();
    };

    canvas._copy = async () => {
        try {
            const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
            await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
            return true;
        } catch { return false; }
    };
}

export function clear(canvas) { if (canvas?._clear) canvas._clear(); }
export function getDataUrl(canvas, format, quality) {
    return canvas?._getDataUrl ? canvas._getDataUrl(format, quality) : '';
}
export function getTrimmedDataUrl(canvas, format, quality) {
    return canvas?._getTrimmedDataUrl ? canvas._getTrimmedDataUrl(format, quality) : '';
}
export function isEmpty(canvas) { return canvas?._isEmpty ? canvas._isEmpty() : true; }
export function getStrokeCount(canvas) { return canvas?._getStrokeCount ? canvas._getStrokeCount() : 0; }
export function undo(canvas) { return canvas?._undo ? canvas._undo() : false; }
export function redo(canvas) { return canvas?._redo ? canvas._redo() : false; }
export function download(canvas, filename, format, quality) {
    if (canvas?._download) canvas._download(filename, format, quality);
}
export async function copyToClipboard(canvas) { return canvas?._copy ? await canvas._copy() : false; }
export function setPenColor(canvas, color) { if (canvas?._setPenColor) canvas._setPenColor(color); }
export function setPenWidth(canvas, width) { if (canvas?._setPenWidth) canvas._setPenWidth(width); }
export function setReadOnly(canvas, readOnly) { if (canvas?._setReadOnly) canvas._setReadOnly(readOnly); }
export function setBgColor(canvas, color) { if (canvas?._setBgColor) canvas._setBgColor(color); }
export function resize(canvas) { if (canvas?._resize) canvas._resize(); }
export function loadImage(canvas, dataUrl) { return canvas?._loadImage ? canvas._loadImage(dataUrl) : Promise.resolve(false); }
export function replay(canvas, strokeIndex) { return canvas?._replay ? canvas._replay(strokeIndex) : Promise.resolve(false); }

export function dispose(canvas) {
    if (!canvas) return;
    const h = canvas._handlers;
    if (h) {
        try { canvas.removeEventListener('mousedown', h.start); } catch {}
        try { canvas.removeEventListener('mousemove', h.draw); } catch {}
        try { window.removeEventListener('mouseup', h.stop); } catch {}
        try { canvas.removeEventListener('mouseleave', h.stop); } catch {}
        try { canvas.removeEventListener('touchstart', h.onTouchStart); } catch {}
        try { canvas.removeEventListener('touchmove', h.onTouchMove); } catch {}
        try { canvas.removeEventListener('touchend', h.stop); } catch {}
        try { canvas.removeEventListener('touchcancel', h.stop); } catch {}
    }
    if (canvas._resizeObserver) {
        try { canvas._resizeObserver.disconnect(); } catch {}
    }
    for (const key of ['_handlers', '_clear', '_getDataUrl', '_getTrimmedDataUrl', '_isEmpty',
        '_getStrokeCount', '_undo', '_redo', '_download', '_copy', '_setPenColor',
        '_setPenWidth', '_setReadOnly', '_setBgColor', '_resize', '_loadImage',
        '_replay', '_resizeObserver']) {
        canvas[key] = null;
    }
}
