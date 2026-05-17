// SgLeaflet - Leaflet.js Integration Module for SuperUI Blazor

const _instances = new Map();
const _loaded    = new Set();

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url || _loaded.has(url)) return Promise.resolve();
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
    if (!url || document.querySelector(`link[href="${url}"]`)) return;
    const l = document.createElement('link');
    l.rel = 'stylesheet'; l.href = url;
    document.head.appendChild(l);
}

async function _ensureLeaflet(sources) {
    const cssUrl = sources?.leafletCss || 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
    const jsUrl  = sources?.leafletScript || 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
    _loadCss(cssUrl);
    await _loadScript(jsUrl);
    let L = window.L;
    let n = 0;
    while (!L && n++ < 80) { await new Promise(r => setTimeout(r, 100)); L = window.L; }
    if (!L) throw new Error('Leaflet not loaded');

    // Inject popup styles once
    if (!document.getElementById('sg-leaflet-styles')) {
        const style = document.createElement('style');
        style.id = 'sg-leaflet-styles';
        style.textContent = `
            .sg-leaflet-popup-title { font-weight:600; font-size:13px; margin-bottom:3px; color:#111827; }
            .sg-leaflet-popup-desc  { font-size:11px; color:#6b7280; margin-bottom:3px; }
            .sg-leaflet-popup-coords{ font-size:10px; color:#9ca3af; font-family:monospace; }
            .leaflet-popup-content-wrapper { border-radius:8px !important; box-shadow:0 4px 16px rgba(0,0,0,0.12) !important; }
            .leaflet-popup-content { margin:10px 14px !important; }
        `;
        document.head.appendChild(style);
    }

    return L;
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Tile layer URL builder ────────────────────────────────────────────────────

function _tileLayerConfig(opts) {
    switch (opts.tileLayer) {
        case 'CartoDB_Positron':
            return {
                url: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
                options: { attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/">CARTO</a>', subdomains: 'abcd', maxZoom: 20 }
            };
        case 'CartoDB_DarkMatter':
            return {
                url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
                options: { attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/">CARTO</a>', subdomains: 'abcd', maxZoom: 20 }
            };
        case 'Stamen_Toner':
            return {
                url: 'https://stamen-tiles.a.ssl.fastly.net/toner/{z}/{x}/{y}.png',
                options: { attribution: 'Map tiles by <a href="http://stamen.com">Stamen Design</a>', maxZoom: 20 }
            };
        case 'Stamen_Watercolor':
            return {
                url: 'https://stamen-tiles.a.ssl.fastly.net/watercolor/{z}/{x}/{y}.jpg',
                options: { attribution: 'Map tiles by <a href="http://stamen.com">Stamen Design</a>', maxZoom: 18 }
            };
        case 'Esri_WorldImagery':
            return {
                url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
                options: { attribution: 'Tiles © Esri — Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community', maxZoom: 19 }
            };
        case 'Custom':
            return {
                url: opts.customTileUrl || '',
                options: { maxZoom: opts.maxZoom || 19 }
            };
        case 'OpenStreetMap':
        default:
            return {
                url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                options: { attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors', maxZoom: 19 }
            };
    }
}

// ── Marker icon (canvas-based pin with tail) ──────────────────────────────────

function _markerIcon(L, color, emoji, size) {
    const accent = _cssVar('--sg-color-primary', '#006fee');
    const c      = color  || accent;
    const s      = size   || 32;
    const icon   = emoji  || '';

    const canvas = document.createElement('canvas');
    canvas.width  = s;
    canvas.height = s + 8; // extra for pin tail
    const ctx = canvas.getContext('2d');

    // Pin body (circle)
    const r  = s / 2 - 2;
    const cx = s / 2, cy = s / 2;
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fillStyle = c;
    ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.9)';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Pin tail
    ctx.beginPath();
    ctx.moveTo(cx - 5, cy + r - 2);
    ctx.lineTo(cx, cy + r + 8);
    ctx.lineTo(cx + 5, cy + r - 2);
    ctx.fillStyle = c;
    ctx.fill();

    // Icon / text
    if (icon) {
        ctx.font = `${Math.round(s * 0.45)}px sans-serif`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillStyle = '#ffffff';
        ctx.fillText(icon, cx, cy);
    }

    return L.icon({
        iconUrl: canvas.toDataURL(),
        iconSize:   [s, s + 8],
        iconAnchor: [s / 2, s + 8],
        popupAnchor:[0, -(s + 8)],
    });
}

// ── Waypoint marker (circle with letter A/B) ──────────────────────────────────

function _waypointIcon(L, color, letter) {
    const size = 28;
    const cv = document.createElement('canvas');
    cv.width = size; cv.height = size;
    const ctx = cv.getContext('2d');
    ctx.beginPath();
    ctx.arc(size / 2, size / 2, size / 2 - 2, 0, Math.PI * 2);
    ctx.fillStyle = color || '#2563eb';
    ctx.fill();
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${Math.round(size * 0.45)}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(letter, size / 2, size / 2);
    return L.icon({
        iconUrl: cv.toDataURL(),
        iconSize:   [size, size],
        iconAnchor: [size / 2, size / 2],
        popupAnchor:[0, -size / 2],
    });
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, circles, sources) {
    await disposeMap(instanceId);

    const L = await _ensureLeaflet(sources);

    // ── Map ──
    const mapOpts = {
        center:    [opts.centerLat ?? 55.751, opts.centerLon ?? 37.618],
        zoom:      opts.zoom    ?? 10,
        minZoom:   opts.minZoom ?? 2,
        maxZoom:   opts.maxZoom ?? 19,
        zoomControl:       false,
        attributionControl: false,
        scrollWheelZoom:   opts.mouseWheelZoom !== false,
    };

    const map = L.map(containerRef, mapOpts);

    // ── Tile layer ──
    const tlCfg = _tileLayerConfig(opts);
    L.tileLayer(tlCfg.url, tlCfg.options).addTo(map);

    // ── Controls ──
    if (opts.showZoomControl  !== false) L.control.zoom({ position: 'topright' }).addTo(map);
    if (opts.showAttribution  !== false) L.control.attribution({ position: 'bottomright' }).addTo(map);
    if (opts.showScaleControl !== false) L.control.scale({ position: 'bottomleft', imperial: false }).addTo(map);

    // ── Marker layer group ──
    const markerGroup = L.layerGroup().addTo(map);
    _addMarkers(L, markerGroup, markers ?? [], opts, dotnetRef);

    // ── Polyline layer group ──
    const polylineGroup = L.layerGroup().addTo(map);
    _addPolylines(L, polylineGroup, polylines ?? []);

    // ── Polygon layer group ──
    const polygonGroup = L.layerGroup().addTo(map);
    _addPolygons(L, polygonGroup, polygons ?? []);

    // ── Circle layer group ──
    const circleGroup = L.layerGroup().addTo(map);
    _addCircles(L, circleGroup, circles ?? []);

    // ── Route layer group (for drawRoute / clearRoute) ──
    const routeGroup = L.layerGroup().addTo(map);
    const routeLayers = new Map(); // routeId → [layers]

    // ── Map click handler ──
    map.on('click', (e) => {
        try {
            dotnetRef.invokeMethodAsync('OnMapClickedAsync', {
                latitude:  e.latlng.lat,
                longitude: e.latlng.lng,
            });
        } catch {}
    });

    // ── View change handler ──
    map.on('moveend zoomend', () => {
        try {
            const c = map.getCenter();
            dotnetRef.invokeMethodAsync('OnViewChangedAsync', {
                centerLat: c.lat,
                centerLon: c.lng,
                zoom:      map.getZoom(),
            });
        } catch {}
    });

    // ── Fit to markers ──
    if (opts.fitToMarkers) {
        const allMarkers = markers ?? [];
        if (allMarkers.length > 0) {
            const latlngs = allMarkers.map(m => [m.latitude, m.longitude]);
            map.fitBounds(L.latLngBounds(latlngs), { padding: [40, 40], maxZoom: 15 });
        }
    }

    // ── Resize observer ──
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { try { map.invalidateSize(); } catch {} });
        });
        ro.observe(containerRef);
    }

    // ── Fix Leaflet z-index so it doesn't overlap page header ──
    // Leaflet pane z-indexes start at 200; we keep them but ensure container is relative
    containerRef.style.position = 'relative';

    _instances.set(instanceId, { map, L, markerGroup, polylineGroup, polygonGroup, circleGroup, routeGroup, routeLayers, dotnetRef, opts, ro });
}

// ── Internal helpers ──────────────────────────────────────────────────────────

function _addMarkers(L, group, markers, opts, dotnetRef) {
    (markers ?? []).forEach(m => {
        const icon   = _markerIcon(L, m.color, m.icon, m.size);
        const marker = L.marker([m.latitude, m.longitude], { icon });

        if (opts.showPopup !== false && (m.title || m.description)) {
            const html = `<div class="sg-leaflet-popup-title">${m.title ?? ''}</div>` +
                         (m.description ? `<div class="sg-leaflet-popup-desc">${m.description}</div>` : '') +
                         `<div class="sg-leaflet-popup-coords">${m.latitude.toFixed(5)}, ${m.longitude.toFixed(5)}</div>`;
            marker.bindPopup(html, { maxWidth: 240 });
        }

        marker.on('click', (e) => {
            L.DomEvent.stopPropagation(e);
            try {
                dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', {
                    markerId:    String(m.id),
                    title:       m.title       ?? null,
                    description: m.description ?? null,
                    latitude:    m.latitude,
                    longitude:   m.longitude,
                    data:        m.data        ?? null,
                });
            } catch {}
        });

        marker.addTo(group);
    });
}

function _addPolylines(L, group, polylines) {
    (polylines ?? []).forEach(p => {
        const latlngs = (p.coordinates ?? []).map(c => [c.latitude, c.longitude]);
        if (latlngs.length < 2) return;
        const style = {
            color:     p.color ?? '#2563eb',
            weight:    p.width ?? 3,
            opacity:   0.9,
            dashArray: p.dashed ? '8, 6' : undefined,
            lineCap:   'round',
            lineJoin:  'round',
        };
        L.polyline(latlngs, style).addTo(group);
    });
}

function _addPolygons(L, group, polygons) {
    (polygons ?? []).forEach(p => {
        const latlngs = (p.coordinates ?? []).map(c => [c.latitude, c.longitude]);
        if (latlngs.length < 3) return;
        const poly = L.polygon(latlngs, {
            color:       p.strokeColor ?? '#2563eb',
            weight:      p.strokeWidth ?? 2,
            fillColor:   p.fillColor   ?? 'rgba(37,99,235,0.2)',
            fillOpacity: 0.35,
            opacity:     0.9,
        });
        if (p.title) poly.bindTooltip(p.title);
        poly.addTo(group);
    });
}

function _addCircles(L, group, circles) {
    (circles ?? []).forEach(c => {
        const circle = L.circle([c.latitude, c.longitude], {
            radius:      c.radius    ?? 500,
            color:       c.color     ?? '#2563eb',
            fillColor:   c.fillColor ?? 'rgba(37,99,235,0.15)',
            fillOpacity: 0.4,
            weight:      2,
            opacity:     0.9,
        });
        if (c.title) circle.bindTooltip(c.title);
        circle.addTo(group);
    });
}

// ── Exported functions ────────────────────────────────────────────────────────

export function updateMarkers(instanceId, markers) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.markerGroup.clearLayers();
    _addMarkers(inst.L, inst.markerGroup, markers ?? [], inst.opts, inst.dotnetRef);
}

export function setTileLayer(instanceId, tileLayerName, customUrl) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    // Remove existing tile layers
    inst.map.eachLayer(layer => {
        if (layer instanceof inst.L.TileLayer) {
            inst.map.removeLayer(layer);
        }
    });
    const tlCfg = _tileLayerConfig({ tileLayer: tileLayerName, customTileUrl: customUrl });
    inst.L.tileLayer(tlCfg.url, tlCfg.options).addTo(inst.map);
}

export function setCenter(instanceId, lat, lon, zoom) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.setView([lat, lon], zoom ?? inst.map.getZoom(), { animate: true, duration: 0.4 });
}

export function fitToMarkers(instanceId, padding) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const layers = [];
    inst.markerGroup.eachLayer(l => layers.push(l));
    if (layers.length === 0) return;
    const group = inst.L.featureGroup(layers);
    inst.map.fitBounds(group.getBounds(), { padding: [padding ?? 40, padding ?? 40], maxZoom: 15, animate: true });
}

export function fitToBounds(instanceId, south, west, north, east) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.fitBounds([[south, west], [north, east]], { padding: [40, 40], animate: true });
}

export function drawRoute(instanceId, routeId, coordinates, style) {
    _drawRouteLines(instanceId, routeId, coordinates, style);
}

// ── Real routing via GraphHopper (primary) + straight line fallback ───────────

export async function buildRoute(instanceId, routeId, fromLat, fromLon, toLat, toLon, style, alternatives) {
    const inst = _instances.get(instanceId);
    if (!inst) return { ok: false, error: 'Map not initialized', routes: [] };

    // Clear previous
    clearRoute(instanceId, routeId);

    // ── 1. GraphHopper (free, real roads) ──
    try {
        const body = {
            points:         [[fromLon, fromLat], [toLon, toLat]],
            profile:        'car',
            locale:         'ru',
            instructions:   true,
            calc_points:    true,
            points_encoded: false,
            alternative_route: alternatives
                ? { max_paths: 3, max_weight_factor: 1.4, max_share_factor: 0.6 }
                : undefined,
        };
        const resp = await fetch('https://graphhopper.com/api/1/route', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(body),
        });
        if (resp.ok) {
            const data = await resp.json();
            if (data.paths?.length) {
                const routes = [];
                const { L, routeGroup, routeLayers } = inst;
                const added = [];

                data.paths.forEach((path, idx) => {
                    const isMain = idx === 0;
                    const color  = style?.color ?? '#2563eb';
                    const width  = style?.width ?? 5;
                    const latlngs = path.points.coordinates.map(c => [c[1], c[0]]);

                    if (!isMain) {
                        // Alternative routes — thinner, grey, dashed
                        const altLine = L.polyline(latlngs, {
                            color: '#94a3b8', weight: 3, opacity: 0.7,
                            dashArray: '8, 6', lineCap: 'round', lineJoin: 'round',
                        });
                        altLine.addTo(routeGroup);
                        added.push(altLine);
                    } else {
                        // Main route — shadow + colored line
                        const shadow = L.polyline(latlngs, {
                            color: 'rgba(0,0,0,0.18)', weight: width + 4, opacity: 1,
                            lineCap: 'round', lineJoin: 'round',
                        });
                        shadow.addTo(routeGroup);
                        added.push(shadow);

                        const line = L.polyline(latlngs, {
                            color, weight: width, opacity: 0.95,
                            dashArray: style?.dashed ? '10, 8' : undefined,
                            lineCap: 'round', lineJoin: 'round',
                        });
                        line.addTo(routeGroup);
                        added.push(line);

                        // Fit map to route
                        if (path.bbox) {
                            const [minLon, minLat, maxLon, maxLat] = path.bbox;
                            inst.map.fitBounds([[minLat, minLon], [maxLat, maxLon]], { padding: [60, 60], animate: true });
                        } else {
                            inst.map.fitBounds(line.getBounds(), { padding: [60, 60], animate: true });
                        }
                    }

                    const distM  = path.distance;
                    const durSec = path.time / 1000;
                    routes.push({
                        distanceKm:   distM / 1000,
                        distanceText: distM >= 1000 ? `${(distM/1000).toFixed(1)} км` : `${Math.round(distM)} м`,
                        durationMin:  Math.round(durSec / 60),
                        durationText: durSec >= 3600
                            ? `${Math.floor(durSec/3600)} ч ${Math.floor((durSec%3600)/60)} мин`
                            : `${Math.floor(durSec/60)} мин`,
                        steps: (path.instructions ?? []).map(i => ({
                            instruction: i.text ?? '',
                            distance:    distM >= 1000 ? `${(i.distance/1000).toFixed(1)} км` : `${Math.round(i.distance)} м`,
                            duration:    '',
                        })),
                    });
                });

                // A/B waypoint markers (on top)
                const color = style?.color ?? '#2563eb';
                const startM = L.marker([fromLat, fromLon], { icon: _waypointIcon(L, color, 'A'), zIndexOffset: 1000 });
                startM.addTo(routeGroup); added.push(startM);
                const endM = L.marker([toLat, toLon], { icon: _waypointIcon(L, '#dc2626', 'B'), zIndexOffset: 1000 });
                endM.addTo(routeGroup); added.push(endM);

                routeLayers.set(routeId, added);
                return { ok: true, straight: false, selectedIndex: 0, routes };
            }
        }
    } catch {}

    // ── 2. Straight line fallback ──
    const coords = [{ latitude: fromLat, longitude: fromLon }, { latitude: toLat, longitude: toLon }];
    _drawRouteLines(instanceId, routeId, coords, style);
    const dist = _haversine(fromLat, fromLon, toLat, toLon);
    return {
        ok: true, straight: true, selectedIndex: 0,
        routes: [{ distanceKm: dist, distanceText: `${dist.toFixed(1)} км`, durationMin: null, durationText: null, steps: [] }],
    };
}

// ── Internal: draw raw coordinate array as route ──────────────────────────────

function _drawRouteLines(instanceId, routeId, coordinates, style) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { L, routeGroup, routeLayers } = inst;

    if (routeLayers.has(routeId)) {
        routeLayers.get(routeId).forEach(l => routeGroup.removeLayer(l));
        routeLayers.delete(routeId);
    }

    if (!coordinates || coordinates.length < 2) return;

    const color  = style?.color  ?? '#2563eb';
    const width  = style?.width  ?? 4;
    const dashed = style?.dashed ?? false;
    const latlngs = coordinates.map(c => [c.latitude, c.longitude]);
    const added   = [];

    const shadow = L.polyline(latlngs, {
        color: 'rgba(0,0,0,0.18)', weight: width + 4, opacity: 1,
        lineCap: 'round', lineJoin: 'round',
    });
    shadow.addTo(routeGroup); added.push(shadow);

    const line = L.polyline(latlngs, {
        color, weight: width, opacity: 0.95,
        dashArray: dashed ? '10, 8' : undefined,
        lineCap: 'round', lineJoin: 'round',
    });
    line.addTo(routeGroup); added.push(line);

    const startM = L.marker(latlngs[0], { icon: _waypointIcon(L, color, 'A'), zIndexOffset: 1000 });
    startM.addTo(routeGroup); added.push(startM);
    const endM = L.marker(latlngs[latlngs.length - 1], { icon: _waypointIcon(L, '#dc2626', 'B'), zIndexOffset: 1000 });
    endM.addTo(routeGroup); added.push(endM);

    routeLayers.set(routeId, added);
}

function _haversine(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2)**2 + Math.cos(lat1*Math.PI/180)*Math.cos(lat2*Math.PI/180)*Math.sin(dLon/2)**2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
}

export function clearRoute(instanceId, routeId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { routeGroup, routeLayers } = inst;
    if (routeLayers.has(routeId)) {
        routeLayers.get(routeId).forEach(l => routeGroup.removeLayer(l));
        routeLayers.delete(routeId);
    }
}

export function exportPng(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try {
        // Collect all tile canvases and merge them
        const container = inst.map.getContainer();
        const canvases  = container.querySelectorAll('canvas');
        if (canvases.length === 0) return;

        const size   = inst.map.getSize();
        const merged = document.createElement('canvas');
        merged.width  = size.x;
        merged.height = size.y;
        const ctx = merged.getContext('2d');

        canvases.forEach(c => {
            try { ctx.drawImage(c, 0, 0); } catch {}
        });

        const a = document.createElement('a');
        a.href     = merged.toDataURL('image/png');
        a.download = `leaflet-map-${Date.now()}.png`;
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    } catch {}
}

export async function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.map.remove(); } catch {}
    _instances.delete(instanceId);
}
