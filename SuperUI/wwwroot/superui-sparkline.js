// superui-sparkline.js - Minimal sparkline chart using Canvas API

const sparklines = new Map();

export async function initSparkline(canvas, config) {
    if (!canvas || !config?.data) return;
    
    // Cleanup old sparkline if exists
    const oldId = canvas.id;
    if (oldId && sparklines.has(oldId)) {
        disposeSparkline(oldId);
    }
    
    const id = canvas.id || `spark-${Date.now()}`;
    canvas.id = id;
    
    const ctx = canvas.getContext('2d');
    const data = config.data;
    const color = config.color || '#6b7280';
    const fill = config.fill !== false;
    const height = config.height || 40;
    
    // Set canvas dimensions
    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width * (window.devicePixelRatio || 1);
    canvas.height = height * (window.devicePixelRatio || 1);
    canvas.style.height = `${height}px`;
    
    ctx.scale(window.devicePixelRatio || 1, window.devicePixelRatio || 1);
    
    drawSparkline(ctx, data, color, fill, rect.width, height);
    
    sparklines.set(id, { canvas, data, color, fill, height });
}

export function updateSparkline(id, data, color) {
    const item = sparklines.get(id);
    if (!item) return;
    
    const { canvas } = item;
    const ctx = canvas.getContext('2d');
    const rect = canvas.getBoundingClientRect();
    const height = item.height || 40;
    
    ctx.clearRect(0, 0, rect.width, height);
    drawSparkline(ctx, data, color || item.color, item.fill, rect.width, height);
    
    item.data = data;
    if (color) item.color = color;
}

export function disposeSparkline(id) {
    const item = sparklines.get(id);
    if (item) {
        const { canvas } = item;
        const ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
        sparklines.delete(id);
    }
}

function drawSparkline(ctx, data, color, fill, width, height) {
    if (!data || data.length < 2) return;
    
    const padding = 2;
    const chartWidth = width - padding * 2;
    const chartHeight = height - padding * 2;
    
    const min = Math.min(...data);
    const max = Math.max(...data);
    const range = max - min || 1;
    
    const points = data.map((value, index) => ({
        x: padding + (index / (data.length - 1)) * chartWidth,
        y: padding + chartHeight - ((value - min) / range) * chartHeight
    }));
    
    // Draw fill if enabled
    if (fill) {
        ctx.beginPath();
        ctx.moveTo(points[0].x, height);
        
        points.forEach(p => ctx.lineTo(p.x, p.y));
        
        ctx.lineTo(points[points.length - 1].x, height);
        ctx.closePath();
        
        const fillColor = color + '33'; // Add alpha
        ctx.fillStyle = fillColor;
        ctx.fill();
    }
    
    // Draw line
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    
    // Use smooth curve
    for (let i = 1; i < points.length; i++) {
        const prev = points[i - 1];
        const curr = points[i];
        const cpx = (prev.x + curr.x) / 2;
        ctx.bezierCurveTo(cpx, prev.y, cpx, curr.y, curr.x, curr.y);
    }
    
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.stroke();
    
    // Draw last point
    const last = points[points.length - 1];
    ctx.beginPath();
    ctx.arc(last.x, last.y, 2.5, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
}