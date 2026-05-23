export function init(canvas, dotNetRef, color) {
    const ctx = canvas.getContext('2d');
    let drawing = false;

    // Adjust canvas resolution to match display size
    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;

    function getPos(e) {
        const r = canvas.getBoundingClientRect();
        if (e.touches) {
            return {
                x: e.touches[0].clientX - r.left,
                y: e.touches[0].clientY - r.top
            };
        }
        return {
            x: e.offsetX,
            y: e.offsetY
        };
    }

    function start(e) {
        drawing = true;
        const pos = getPos(e);
        ctx.beginPath();
        ctx.moveTo(pos.x, pos.y);
        ctx.strokeStyle = color;
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
    }

    function draw(e) {
        if (!drawing) return;
        const pos = getPos(e);
        ctx.lineTo(pos.x, pos.y);
        ctx.stroke();
    }

    function stop() {
        drawing = false;
    }

    const onTouchStart = (e) => { e.preventDefault(); start(e); };
    const onTouchMove  = (e) => { e.preventDefault(); draw(e);  };

    canvas.addEventListener('mousedown', start);
    canvas.addEventListener('mousemove', draw);
    window.addEventListener('mouseup', stop);

    canvas.addEventListener('touchstart', onTouchStart, { passive: false });
    canvas.addEventListener('touchmove',  onTouchMove,  { passive: false });
    canvas.addEventListener('touchend', stop);

    canvas._handlers = { start, draw, stop, onTouchStart, onTouchMove };

    canvas._clear = () => ctx.clearRect(0, 0, canvas.width, canvas.height);
    canvas._getDataUrl = () => canvas.toDataURL('image/png');

    canvas._download = (filename) => {
        const link = document.createElement('a');
        link.download = filename || 'signature.png';
        link.href = canvas.toDataURL('image/png');
        link.click();
    };

    canvas._copy = async () => {
        try {
            const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
            await navigator.clipboard.write([
                new ClipboardItem({ 'image/png': blob })
            ]);
            return true;
        } catch (err) {
            console.error('Failed to copy image: ', err);
            return false;
        }
    };
}

export function clear(canvas) { canvas._clear(); }
export function getDataUrl(canvas) { return canvas._getDataUrl(); }
export function download(canvas, filename) { canvas._download(filename); }
export async function copyToClipboard(canvas) { return await canvas._copy(); }

export function dispose(canvas) {
    if (!canvas) return;
    const h = canvas._handlers;
    if (h) {
        try { canvas.removeEventListener('mousedown', h.start); } catch {}
        try { canvas.removeEventListener('mousemove', h.draw);  } catch {}
        try { window.removeEventListener('mouseup',   h.stop);  } catch {}
        try { canvas.removeEventListener('touchstart', h.onTouchStart); } catch {}
        try { canvas.removeEventListener('touchmove',  h.onTouchMove);  } catch {}
        try { canvas.removeEventListener('touchend',   h.stop);         } catch {}
    }
    canvas._handlers = null;
    canvas._clear = null;
    canvas._getDataUrl = null;
    canvas._download = null;
    canvas._copy = null;
}
