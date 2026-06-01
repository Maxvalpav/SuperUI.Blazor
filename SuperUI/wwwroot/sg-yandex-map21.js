// SgYandexMap21 — Yandex Maps JavaScript API 2.1 wrapper for SuperUI Blazor
// API URL: https://api-maps.yandex.ru/2.1/?apikey=KEY&lang=ru_RU

const _instances = new Map();
let   _ymapsLoading = false;
let   _ymapsReady   = false;
let   _ymapsLoadKey = null;
const _ymapsQueue   = [];

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadYmaps21(apiKey, lang) {
    return new Promise((resolve, reject) => {
        // Already loaded with same key — reuse
        if (_ymapsReady && window.ymaps && _ymapsLoadKey === (apiKey || '')) {
            resolve(window.ymaps); return;
        }

        // Key changed — reset
        if (_ymapsLoadKey !== null && _ymapsLoadKey !== (apiKey || '')) {
            _ymapsLoading = false; _ymapsReady = false; _ymapsLoadKey = null;
            const old = document.getElementById('sg-ymaps21-script');
            if (old) old.remove();
        }

        _ymapsQueue.push({ resolve, reject });
        if (_ymapsLoading) return;
        _ymapsLoading = true;
        _ymapsLoadKey = apiKey || '';

        const key = apiKey ? `&apikey=${encodeURIComponent(apiKey)}` : '';
        const loc = lang  ? `&lang=${lang}` : '&lang=ru_RU';
        const s   = document.createElement('script');
        s.id   = 'sg-ymaps21-script';
        s.src  = `https://api-maps.yandex.ru/2.1/?${key}${loc}`;
        s.type = 'text/javascript';
        s.onload = () => {
            if (!window.ymaps) {
                const err = new Error('ymaps not found after script load');
                _ymapsLoading = false;
                _ymapsQueue.forEach(p => p.reject(err)); _ymapsQueue.length = 0;
                return;
            }
            window.ymaps.ready(() => {
                _ymapsReady = true;
                const q = _ymapsQueue.splice(0);
                q.forEach(p => p.resolve(window.ymaps));
            });
        };
        s.onerror = () => {
            _ymapsLoading = false;
            const err = new Error('Не удалось загрузить Яндекс Карты 2.1. Проверьте API-ключ и ограничения домена.');
            _ymapsQueue.forEach(p => p.reject(err)); _ymapsQueue.length = 0;
        };
        document.head.appendChild(s);
    });
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Canvas marker ─────────────────────────────────────────────────────────────

function _makeMarkerIcon(color, emoji) {
    const c = color ?? _cssVar('--sg-color-primary', '#006fee');
    const size = 36;
    const cv = document.createElement('canvas');
    cv.width = size; cv.height = size + 8;
    const ctx = cv.getContext('2d');
    const r = size / 2 - 2, cx = size / 2, cy = size / 2;
    ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fillStyle = c; ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.9)'; ctx.lineWidth = 2; ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(cx - 5, cy + r - 2); ctx.lineTo(cx, cy + r + 8); ctx.lineTo(cx + 5, cy + r - 2);
    ctx.fillStyle = c; ctx.fill();
    if (emoji) {
        ctx.font = `${Math.round(size * 0.42)}px sans-serif`;
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillStyle = '#fff'; ctx.fillText(emoji, cx, cy);
    }
    return cv.toDataURL();
}

function _makeWaypointIcon(color, letter) {
    const size = 28;
    const cv = document.createElement('canvas');
    cv.width = size; cv.height = size;
    const ctx = cv.getContext('2d');
    ctx.beginPath(); ctx.arc(size/2, size/2, size/2 - 2, 0, Math.PI * 2);
    ctx.fillStyle = color; ctx.fill();
    ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${Math.round(size * 0.45)}px sans-serif`;
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(letter, size/2, size/2);
    return cv.toDataURL();
}

// ── Map type ──────────────────────────────────────────────────────────────────

function _mapType(type) {
    const map = { satellite: 'yandex#satellite', hybrid: 'yandex#hybrid', map: 'yandex#map' };
    return map[type] ?? 'yandex#map';
}

// ── Init ──────────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons) {
    await disposeMap(instanceId);

    const ymaps = await _loadYmaps21(opts.apiKey, opts.lang);

    const map = new ymaps.Map(containerRef, {
        center:   [opts.centerLat ?? 55.751, opts.centerLon ?? 37.618],
        zoom:     opts.zoom ?? 10,
        type:     _mapType(opts.mapType),
        controls: opts.showControls ? ['zoomControl', 'fullscreenControl', 'geolocationControl'] : [],
    }, { suppressMapOpenBlock: true });

    // ── Traffic ──
    let trafficObj = null;
    if (opts.showTraffic) {
        trafficObj = new ymaps.traffic.provider.Actual({}, { infoLayerShown: true });
        trafficObj.setMap(map);
    }

    // ── Markers ──
    const markerObjs = [];
    (markers ?? []).forEach(m => {
        const iconUrl = _makeMarkerIcon(m.color, m.icon);
        const pm = new ymaps.Placemark(
            [m.latitude, m.longitude],
            { balloonContentHeader: m.title ?? '', balloonContentBody: m.description ?? '', hintContent: m.title ?? '' },
            { iconLayout: 'default#image', iconImageHref: iconUrl, iconImageSize: [36, 44], iconImageOffset: [-18, -44] }
        );
        pm.events.add('click', () => {
            try { dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', {
                markerId: String(m.id), title: m.title ?? null, description: m.description ?? null,
                latitude: m.latitude, longitude: m.longitude, data: m.data ?? null,
            })?.catch(() => {}); } catch {}
        });
        map.geoObjects.add(pm);
        markerObjs.push({ pm, data: m });
    });

    // ── Polylines ──
    const polylineObjs = {};
    (polylines ?? []).forEach(p => {
        const coords = p.coordinates.map(c => [c.latitude, c.longitude]);
        const pl = new ymaps.Polyline(coords, {}, {
            strokeColor: p.color ?? '#2563eb', strokeWidth: p.width ?? 3,
            strokeStyle: p.dashed ? 'dash' : 'solid',
        });
        map.geoObjects.add(pl);
        polylineObjs[p.id] = pl;
    });

    // ── Polygons ──
    (polygons ?? []).forEach(p => {
        const coords = [p.coordinates.map(c => [c.latitude, c.longitude])];
        const poly = new ymaps.Polygon(coords, {}, {
            fillColor: p.fillColor ?? '#2563eb26',
            strokeColor: p.strokeColor ?? '#2563eb', strokeWidth: p.strokeWidth ?? 2,
        });
        map.geoObjects.add(poly);
    });

    // ── Events ──
    map.events.add('click', e => {
        const c = e.get('coords');
        try { dotnetRef.invokeMethodAsync('OnMapClickedAsync', { latitude: c[0], longitude: c[1] })?.catch(() => {}); } catch {}
    });
    map.events.add('boundschange', () => {
        const c = map.getCenter();
        try { dotnetRef.invokeMethodAsync('OnViewChangedAsync', { centerLat: c[0], centerLon: c[1], zoom: map.getZoom() })?.catch(() => {}); } catch {}
    });

    // ── Resize ──
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { map.container.fitToViewport(); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { map, ymaps, markerObjs, polylineObjs, dotnetRef, ro, traffic: trafficObj });
}

// ── Public API ────────────────────────────────────────────────────────────────

export function setCenter(instanceId, lat, lon, zoom) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.setCenter([lat, lon], zoom ?? inst.map.getZoom(), { duration: 400 });
}

export function setMapType(instanceId, mapType) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.map.setType(_mapType(mapType)); } catch {}
}

export function setTraffic(instanceId, show) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (show) {
        if (!inst.traffic) {
            inst.traffic = new inst.ymaps.traffic.provider.Actual({}, { infoLayerShown: true });
            inst.traffic.setMap(inst.map);
        }
    } else {
        if (inst.traffic) { inst.traffic.setMap(null); inst.traffic = null; }
    }
}

export function addMarker(instanceId, m) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const iconUrl = _makeMarkerIcon(m.color, m.icon);
    const pm = new inst.ymaps.Placemark(
        [m.latitude, m.longitude],
        { balloonContentHeader: m.title ?? '', hintContent: m.title ?? '' },
        { iconLayout: 'default#image', iconImageHref: iconUrl, iconImageSize: [36, 44], iconImageOffset: [-18, -44] }
    );
    pm.events.add('click', () => {
        try { inst.dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', {
            markerId: String(m.id), title: m.title ?? null, description: m.description ?? null,
            latitude: m.latitude, longitude: m.longitude, data: m.data ?? null,
        })?.catch(() => {}); } catch {}
    });
    inst.map.geoObjects.add(pm);
    inst.markerObjs.push({ pm, data: m });
}

export function clearMarkers(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.markerObjs.forEach(({ pm }) => { try { inst.map.geoObjects.remove(pm); } catch {} });
    inst.markerObjs.length = 0;
}

export function drawPolyline(instanceId, id, coords, style) {
    const inst = _instances.get(instanceId);
    if (!inst || !coords || coords.length < 2) return;
    if (inst.polylineObjs[id]) { try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {} delete inst.polylineObjs[id]; }
    const path = coords.map(c => [c.latitude, c.longitude]);
    const pl = new inst.ymaps.Polyline(path, {}, {
        strokeColor: style?.color ?? '#2563eb', strokeWidth: style?.width ?? 4,
        strokeStyle: style?.dashed ? 'dash' : 'solid',
    });
    inst.map.geoObjects.add(pl);
    inst.polylineObjs[id] = pl;
    try { const b = pl.geometry.getBounds(); if (b) inst.map.setBounds(b, { checkZoomRange: true, zoomMargin: 60, duration: 500 }); } catch {}
}

export function clearPolyline(instanceId, id) {
    const inst = _instances.get(instanceId);
    if (!inst || !inst.polylineObjs[id]) return;
    try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {}
    delete inst.polylineObjs[id];
}

export function fitBounds(instanceId, south, west, north, east) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.map.setBounds([[south, west], [north, east]], { checkZoomRange: true, zoomMargin: 60, duration: 400 }); } catch {}
}

// ── Routing via ymaps.route() ─────────────────────────────────────────────────

export async function buildRoute(instanceId, fromLat, fromLon, toLat, toLon, apiKey, alternatives) {
    const inst = _instances.get(instanceId);
    if (!inst) return { ok: false, error: 'Map not initialized', routes: [] };

    _clearRouteObjects(inst);

    try {
        const ymaps = inst.ymaps;

        // ymaps.route() uses the key from the loaded SDK — no separate routing key needed
        const routeResult = await ymaps.route(
            [[fromLat, fromLon], [toLat, toLon]],
            { routingMode: 'auto', results: alternatives ? 3 : 1 }
        );

        const paths = routeResult.getPaths();
        const pathCount = paths.getLength();

        if (pathCount === 0) {
            return { ok: false, error: 'Маршрут не найден', straight: true, selectedIndex: 0,
                routes: [{ distanceKm: _haversine(fromLat, fromLon, toLat, toLon), distanceText: null, durationMin: null, durationText: null, steps: [] }] };
        }

        // Add to map
        inst.map.geoObjects.add(routeResult);
        inst._routeObj = routeResult;

        // Style paths
        paths.each((path, idx) => {
            const isMain = idx === 0;
            try {
                path.getSegments().each(seg => {
                    seg.options.set({
                        strokeColor: isMain ? '#2563eb' : '#94a3b8',
                        strokeWidth: isMain ? 6 : 3,
                        opacity:     isMain ? 1 : 0.65,
                    });
                });
            } catch {}
        });

        // Fit bounds
        try {
            const bounds = routeResult.getBounds();
            if (bounds) inst.map.setBounds(bounds, { checkZoomRange: true, zoomMargin: 60, duration: 500 });
        } catch {}

        // A/B waypoint markers
        _addWaypointMarker(inst, fromLon, fromLat, '#2563eb', 'A');
        _addWaypointMarker(inst, toLon,   toLat,   '#dc2626', 'B');

        // Collect route info
        const routes = [];
        for (let i = 0; i < pathCount; i++) {
            const path = paths.get(i);
            let distM = 0, durSec = null;
            try { distM = path.getLength() ?? 0; } catch {}
            try { durSec = path.getDuration ? path.getDuration() : null; } catch {}

            const steps = [];
            try {
                path.getSegments().each(seg => {
                    const text = seg.properties.get('text') ?? '';
                    if (text) steps.push({
                        instruction: text,
                        distance:    _formatDist(seg.getLength ? seg.getLength() : 0),
                        duration:    '',
                    });
                });
            } catch {}

            routes.push({
                distanceKm:   distM / 1000,
                distanceText: _formatDist(distM),
                durationMin:  durSec ? Math.round(durSec / 60) : null,
                durationText: durSec ? _formatDur(durSec) : null,
                steps,
            });
        }

        return { ok: true, straight: false, selectedIndex: 0, routes };

    } catch (e) {
        // Fallback: straight line
        drawPolyline(instanceId, '__route_0__', [
            { latitude: fromLat, longitude: fromLon },
            { latitude: toLat,   longitude: toLon   },
        ], { color: '#f59e0b', width: 4, dashed: true });
        return { ok: false, error: String(e?.message ?? e), straight: true, selectedIndex: 0,
            routes: [{ distanceKm: _haversine(fromLat, fromLon, toLat, toLon), distanceText: null, durationMin: null, durationText: null, steps: [] }] };
    }
}

export function selectRoute(instanceId, idx) {
    const inst = _instances.get(instanceId);
    if (!inst?._routeObj) return;
    try {
        const paths = inst._routeObj.getPaths();
        paths.each((path, i) => {
            const isMain = i === idx;
            path.getSegments().each(seg => {
                seg.options.set({
                    strokeColor: isMain ? '#2563eb' : '#94a3b8',
                    strokeWidth: isMain ? 6 : 3,
                    opacity:     isMain ? 1 : 0.65,
                });
            });
        });
    } catch {}
}

export function clearRoute(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    _clearRouteObjects(inst);
}

function _addWaypointMarker(inst, lon, lat, color, letter) {
    if (!inst._waypointMarkers) inst._waypointMarkers = [];
    const pm = new inst.ymaps.Placemark([lat, lon], {},
        { iconLayout: 'default#image', iconImageHref: _makeWaypointIcon(color, letter), iconImageSize: [28, 28], iconImageOffset: [-14, -14] });
    inst.map.geoObjects.add(pm);
    inst._waypointMarkers.push(pm);
}

function _clearRouteObjects(inst) {
    if (inst._routeObj) { try { inst.map.geoObjects.remove(inst._routeObj); } catch {} inst._routeObj = null; }
    if (inst._waypointMarkers) {
        inst._waypointMarkers.forEach(m => { try { inst.map.geoObjects.remove(m); } catch {} });
        inst._waypointMarkers = null;
    }
    ['__route_0__', '__route__'].forEach(id => {
        if (inst.polylineObjs?.[id]) { try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {} delete inst.polylineObjs[id]; }
    });
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function _haversine(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2)**2 + Math.cos(lat1*Math.PI/180)*Math.cos(lat2*Math.PI/180)*Math.sin(dLon/2)**2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
}

function _formatDist(m) { return m >= 1000 ? `${(m/1000).toFixed(1)} км` : `${Math.round(m)} м`; }
function _formatDur(sec) {
    const h = Math.floor(sec / 3600), m = Math.floor((sec % 3600) / 60);
    return h > 0 ? `${h} ч ${m} мин` : `${m} мин`;
}

// ── Dispose ───────────────────────────────────────────────────────────────────

export function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.map.destroy(); } catch {}
    _instances.delete(instanceId);
}
