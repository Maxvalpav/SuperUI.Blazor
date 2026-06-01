// sg-network-trace.js — Network Traceroute Map for SuperUI Blazor
// Uses Leaflet.js (CDN) to render a world map with hop markers and polyline.

const _instances = new Map();

function _esc(v) {
    if (v == null) return '';
    return String(v)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}
const _loaded    = new Set();

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (_loaded.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) { _loaded.add(url); resolve(); return; }
        const s = document.createElement('script');
        s.src = url;
        s.onload  = () => { _loaded.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

function _loadCss(url) {
    if (document.querySelector(`link[href="${url}"]`)) return;
    const l = document.createElement('link');
    l.rel = 'stylesheet'; l.href = url;
    document.head.appendChild(l);
}

async function _ensureLeaflet() {
    _loadCss('https://unpkg.com/leaflet@1.9.4/dist/leaflet.css');
    await _loadScript('https://unpkg.com/leaflet@1.9.4/dist/leaflet.js');
    let L = window.L, n = 0;
    while (!L && n++ < 80) { await new Promise(r => setTimeout(r, 100)); L = window.L; }
    if (!L) throw new Error('Leaflet not loaded');
    return L;
}

// ── Ping color ────────────────────────────────────────────────────────────────

function _pingColor(ms) {
    if (ms < 30)  return '#22c55e';  // green
    if (ms < 100) return '#eab308';  // yellow
    if (ms < 200) return '#f97316';  // orange
    return '#ef4444';                // red
}

// ── Hop marker icon ───────────────────────────────────────────────────────────

function _hopIcon(L, hop, index) {
    const size  = 28;
    const color = hop.isTimeout ? '#9ca3af' : _pingColor(hop.pingMs);
    const cv    = document.createElement('canvas');
    cv.width = size; cv.height = size + 8;
    const ctx = cv.getContext('2d');

    // Circle
    ctx.beginPath();
    ctx.arc(size / 2, size / 2, size / 2 - 2, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.9)';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Tail
    ctx.beginPath();
    ctx.moveTo(size / 2 - 4, size / 2 + size / 2 - 3);
    ctx.lineTo(size / 2, size / 2 + size / 2 + 8);
    ctx.lineTo(size / 2 + 4, size / 2 + size / 2 - 3);
    ctx.fillStyle = color;
    ctx.fill();

    // Hop number
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${Math.round(size * 0.38)}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(String(index), size / 2, size / 2);

    return L.icon({
        iconUrl:    cv.toDataURL(),
        iconSize:   [size, size + 8],
        iconAnchor: [size / 2, size + 8],
        popupAnchor:[0, -(size + 8)],
    });
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function init(dotNetRef, containerRef, instanceId) {
    await dispose(instanceId);

    const L   = await _ensureLeaflet();
    const map = L.map(containerRef, {
        center:          [30, 10],
        zoom:            2,
        zoomControl:     true,
        scrollWheelZoom: true,
    });

    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/">CARTO</a>',
        subdomains:  'abcd',
        maxZoom:     19,
    }).addTo(map);

    // Resize observer
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { try { map.invalidateSize(); } catch {} });
        });
        ro.observe(containerRef);
    }

    const markerGroup   = L.layerGroup().addTo(map);
    const polylineGroup = L.layerGroup().addTo(map);
    const hops          = [];

    _instances.set(instanceId, { L, map, markerGroup, polylineGroup, hops, ro });
}

export function addHop(instanceId, hop) {
    const inst = _instances.get(instanceId);
    if (!inst) return;

    const { L, map, markerGroup, polylineGroup, hops } = inst;

    // Skip timeout hops on map (no coordinates)
    if (!hop.isTimeout && hop.latitude !== 0 && hop.longitude !== 0) {
        const icon   = _hopIcon(L, hop, hop.hopNumber);
        const marker = L.marker([hop.latitude, hop.longitude], { icon });

        const pingText = hop.isTimeout ? 'timeout' : `${hop.pingMs.toFixed(1)} ms`;
        const locText  = [hop.country, hop.city].filter(Boolean).join(', ') || 'Unknown';
        const popup    = `
            <div style="font-family:system-ui,sans-serif;min-width:160px;">
                <div style="font-weight:700;font-size:13px;margin-bottom:4px;">Хоп #${_esc(hop.hopNumber)}</div>
                <div style="font-size:12px;color:#374151;margin-bottom:2px;">
                    <span style="font-family:monospace;">${_esc(hop.ipAddress)}</span>
                </div>
                ${hop.hostName ? `<div style="font-size:11px;color:#6b7280;margin-bottom:2px;">${_esc(hop.hostName)}</div>` : ''}
                <div style="font-size:12px;margin-bottom:2px;">
                    <span style="background:${_pingColor(hop.pingMs)};color:#fff;padding:1px 7px;border-radius:10px;font-size:11px;font-weight:600;">${_esc(pingText)}</span>
                </div>
                <div style="font-size:11px;color:#6b7280;">${_esc(locText)}</div>
                ${hop.isp ? `<div style="font-size:11px;color:#9ca3af;">${_esc(hop.isp)}</div>` : ''}
            </div>`;
        marker.bindPopup(popup, { maxWidth: 220 });
        marker.addTo(markerGroup);

        hops.push([hop.latitude, hop.longitude]);

        // Redraw polyline
        polylineGroup.clearLayers();
        if (hops.length >= 2) {
            // Shadow
            L.polyline(hops, {
                color:    'rgba(0,0,0,0.15)',
                weight:   6,
                lineCap:  'round',
                lineJoin: 'round',
            }).addTo(polylineGroup);
            // Main line
            L.polyline(hops, {
                color:    '#006fee',
                weight:   3,
                opacity:  0.85,
                lineCap:  'round',
                lineJoin: 'round',
                dashArray:'6, 4',
            }).addTo(polylineGroup);
        }

        // Pan to latest hop
        try { map.panTo([hop.latitude, hop.longitude], { animate: true, duration: 0.5 }); } catch {}
    }
}

export function clearMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.markerGroup.clearLayers();
    inst.polylineGroup.clearLayers();
    inst.hops.length = 0;
    try { inst.map.setView([30, 10], 2, { animate: false }); } catch {}
}

export function dispose(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.map.remove(); } catch {}
    _instances.delete(instanceId);
}
