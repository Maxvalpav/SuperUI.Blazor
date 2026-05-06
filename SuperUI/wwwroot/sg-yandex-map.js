// SgYandexMap — Yandex Maps dual-version wrapper (v3 + v2.1 fallback)
// v3 key: developer.tech.yandex.ru (new)
// v2.1 key: developer.tech.yandex.ru (old, UUID format)

const _instances = new Map();

// ── Version detection & loader ────────────────────────────────────────────────

let _apiVersion  = null;  // 'v3' | 'v21'
let _apiReady    = false;
let _apiLoading  = false;
let _apiLoadKey  = null;
let _apiQueue    = [];

function _resetLoader() {
    _apiVersion = null; _apiReady = false; _apiLoading = false; _apiLoadKey = null; _apiQueue = [];
    ['sg-ymaps3-loader','sg-ymaps21-loader'].forEach(id => { const s = document.getElementById(id); if (s) s.remove(); });
    try { delete window.ymaps3; } catch {}
    // Don't delete window.ymaps — v2.1 may still be needed
}

// Returns { version: 'v3'|'v21', api: ymaps3 | ymaps }
function _loadYandexMaps(apiKey, lang) {
    return new Promise((resolve, reject) => {
        if (_apiReady) { resolve({ version: _apiVersion, api: _apiVersion === 'v3' ? window.ymaps3 : window.ymaps }); return; }

        const key = apiKey || '';
        if (_apiLoadKey !== null && _apiLoadKey !== key) _resetLoader();

        _apiQueue.push({ resolve, reject });
        if (_apiLoading) return;
        _apiLoading = true;
        _apiLoadKey = key;

        const loc = lang || 'ru_RU';
        
        // UUID format key = v2.1 key, skip v3 attempt
        const isUuidKey = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(key);

        if (isUuidKey) {
            // Go directly to v2.1
            _tryLoadV21(apiKey, loc).then(ymaps => {
                _apiVersion = 'v21'; _apiReady = true;
                const q = _apiQueue; _apiQueue = [];
                q.forEach(p => p.resolve({ version: 'v21', api: ymaps }));
            }).catch(e => {
                _apiLoading = false; _apiReady = false;
                const q = _apiQueue; _apiQueue = [];
                q.forEach(p => p.reject(e));
            });
        } else {
            // Try v3 first, fallback to v2.1
            _tryLoadV3(apiKey, loc).then(ymaps3 => {
                _apiVersion = 'v3'; _apiReady = true;
                const q = _apiQueue; _apiQueue = [];
                q.forEach(p => p.resolve({ version: 'v3', api: ymaps3 }));
            }).catch(() => {
                _tryLoadV21(apiKey, loc).then(ymaps => {
                    _apiVersion = 'v21'; _apiReady = true;
                    const q = _apiQueue; _apiQueue = [];
                    q.forEach(p => p.resolve({ version: 'v21', api: ymaps }));
                }).catch(e => {
                    _apiLoading = false; _apiReady = false;
                    const q = _apiQueue; _apiQueue = [];
                    q.forEach(p => p.reject(e));
                });
            });
        }
    });
}

function _tryLoadV3(apiKey, lang) {
    return new Promise((resolve, reject) => {
        if (window.ymaps3) { resolve(window.ymaps3); return; }
        const s = document.createElement('script');
        s.id = 'sg-ymaps3-loader';
        const p = new URLSearchParams({ lang });
        if (apiKey) p.set('apikey', apiKey);
        s.src = `https://api-maps.yandex.ru/v3/?${p}`;
        s.async = true;
        s.onload = async () => {
            const t = Date.now();
            while (!window.ymaps3 && Date.now() - t < 6000) await new Promise(r => setTimeout(r, 80));
            if (!window.ymaps3) { reject(new Error('ymaps3 not found')); return; }
            try { if (window.ymaps3.ready) await window.ymaps3.ready; resolve(window.ymaps3); }
            catch (e) { reject(e); }
        };
        s.onerror = () => reject(new Error('v3 script load failed'));
        document.head.appendChild(s);
    });
}

function _tryLoadV21(apiKey, lang) {
    return new Promise((resolve, reject) => {
        if (window.ymaps && window.ymaps.Map) { console.log('[Yandex] Using cached ymaps v2.1'); resolve(window.ymaps); return; }
        console.log('[Yandex] Loading v2.1 API, key:', apiKey ? '***' : 'none');
        const s = document.createElement('script');
        s.id = 'sg-ymaps21-loader';
        const key = apiKey ? `&apikey=${apiKey}` : '';
        s.src = `https://api-maps.yandex.ru/2.1/?lang=${lang}${key}`;
        s.type = 'text/javascript';
        s.onload = () => {
            console.log('[Yandex] v2.1 script loaded, checking ymaps...');
            if (!window.ymaps) { reject(new Error('ymaps not found after script load')); return; }
            window.ymaps.ready(() => {
                console.log('[Yandex] v2.1 ready, Map available:', !!window.ymaps.Map);
                resolve(window.ymaps);
            });
        };
        s.onerror = (e) => {
            console.error('[Yandex] v2.1 script load failed', e);
            reject(new Error('Не удалось загрузить Яндекс Карты. Проверьте API-ключ и ограничения домена.'));
        };
        document.head.appendChild(s);
    });
}

export function resetLoader() { _resetLoader(); }

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Canvas marker icon ────────────────────────────────────────────────────────

function _makeMarkerCanvas(color, emoji) {
    const accent = _cssVar('--sui-accent', '#006fee');
    const c = color ?? accent;
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

function _makeWaypointCanvas(color, letter) {
    const size = 28;
    const cv = document.createElement('canvas');
    cv.width = size; cv.height = size;
    const ctx = cv.getContext('2d');
    ctx.beginPath(); ctx.arc(size/2, size/2, size/2 - 2, 0, Math.PI * 2);
    ctx.fillStyle = color; ctx.fill();
    ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${size * 0.45}px sans-serif`;
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(letter, size/2, size/2);
    return cv.toDataURL();
}

// ── Map type ──────────────────────────────────────────────────────────────────

function _mapType(type) {
    switch (type) {
        case 'satellite': return 'satellite';
        case 'hybrid':    return 'hybrid';
        default:          return 'normal';
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons) {
    await disposeMap(instanceId);

    const { version, api } = await _loadYandexMaps(opts.apiKey, opts.lang);

    if (version === 'v3') {
        return _initMapV3(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, api);
    } else {
        return _initMapV21(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, api);
    }
}

// ── Init v3 ───────────────────────────────────────────────────────────────────

function _initMapV3(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, ymaps3) {
    const { YMap, YMapDefaultSchemeLayer, YMapDefaultFeaturesLayer,
            YMapControls, YMapZoomControl, YMapGeolocationControl,
            YMapMarker, YMapListener, YMapFeature, YMapCollection } = ymaps3;

    const map = new YMap(containerRef, {
        location: { center: [opts.centerLon ?? 37.618, opts.centerLat ?? 55.751], zoom: opts.zoom ?? 10 },
        theme: 'light',
    });

    const schemeLayer = new YMapDefaultSchemeLayer({ customization: [] });
    map.addChild(schemeLayer);
    map.addChild(new YMapDefaultFeaturesLayer());

    if (opts.showControls) {
        const controls = new YMapControls({ position: 'right' });
        controls.addChild(new YMapZoomControl());
        controls.addChild(new YMapGeolocationControl());
        map.addChild(controls);
    }

    const markersCollection = new YMapCollection();
    map.addChild(markersCollection);
    const markerObjs = [];

    (markers ?? []).forEach(m => {
        const el = document.createElement('div');
        el.style.cssText = 'cursor:pointer;';
        const img = document.createElement('img');
        img.src = _makeMarkerCanvas(m.color, m.icon);
        img.style.cssText = 'width:36px;height:44px;display:block;';
        el.appendChild(img);
        el.addEventListener('click', () => {
            try { dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
        });
        const marker = new YMapMarker({ coordinates: [m.longitude, m.latitude], anchor: [0.5, 1] }, el);
        markersCollection.addChild(marker);
        markerObjs.push({ marker, data: m });
    });

    const polylineObjs = {};
    (polylines ?? []).forEach(p => {
        const coords = p.coordinates.map(c => [c.longitude, c.latitude]);
        const feature = new YMapFeature({ id: p.id, geometry: { type: 'LineString', coordinates: coords }, style: { stroke: [{ color: p.color ?? '#2563eb', width: p.width ?? 3 }] } });
        map.addChild(feature);
        polylineObjs[p.id] = feature;
    });

    (polygons ?? []).forEach(p => {
        const coords = [p.coordinates.map(c => [c.longitude, c.latitude])];
        const feature = new YMapFeature({ id: p.id, geometry: { type: 'Polygon', coordinates: coords }, style: { fill: p.fillColor ?? 'rgba(37,99,235,0.2)', stroke: [{ color: p.strokeColor ?? '#2563eb', width: p.strokeWidth ?? 2 }] } });
        map.addChild(feature);
    });

    const listener = new YMapListener({
        layer: 'any',
        onClick: (obj, event) => {
            if (obj) return;
            const c = event.coordinates;
            try { dotnetRef.invokeMethodAsync('OnMapClickedAsync', { latitude: c[1], longitude: c[0] }); } catch {}
        },
        onUpdate: () => {
            const loc = map.location;
            try { dotnetRef.invokeMethodAsync('OnViewChangedAsync', { centerLat: loc.center[1], centerLon: loc.center[0], zoom: loc.zoom }); } catch {}
        },
    });
    map.addChild(listener);

    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { map.update({}); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { map, ymaps3, markersCollection, markerObjs, polylineObjs, dotnetRef, ro, schemeLayer, version: 'v3' });
}

// ── Init v2.1 ─────────────────────────────────────────────────────────────────

function _initMapV21(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, ymaps) {
    console.log('[Yandex] initMapV21 starting, container:', containerRef?.id || 'no-id');
    const mapTypeMap = { satellite: 'yandex#satellite', hybrid: 'yandex#hybrid', map: 'yandex#map' };
    const mapType = mapTypeMap[opts.mapType] ?? 'yandex#map';

    if (!containerRef) {
        console.error('[Yandex] Container ref is null!');
        throw new Error('Container element not found');
    }

    let map;
    try {
        map = new ymaps.Map(containerRef, {
            center: [opts.centerLat ?? 55.751, opts.centerLon ?? 37.618],
            zoom:   opts.zoom ?? 10,
            type:   mapType,
            controls: opts.showControls ? ['zoomControl', 'fullscreenControl', 'geolocationControl'] : [],
        }, { suppressMapOpenBlock: true });
        console.log('[Yandex] Map created successfully');
    } catch (e) {
        console.error('[Yandex] Map creation failed:', e);
        throw e;
    }

    const markerObjs = [];
    (markers ?? []).forEach(m => {
        const iconUrl = _makeMarkerCanvas(m.color, m.icon);
        const pm = new ymaps.Placemark([m.latitude, m.longitude],
            { balloonContentHeader: m.title ?? '', hintContent: m.title ?? '' },
            { iconLayout: 'default#image', iconImageHref: iconUrl, iconImageSize: [36, 44], iconImageOffset: [-18, -44] });
        pm.events.add('click', () => {
            try { dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
        });
        map.geoObjects.add(pm);
        markerObjs.push({ marker: pm, data: m });
    });

    const polylineObjs = {};
    (polylines ?? []).forEach(p => {
        const coords = p.coordinates.map(c => [c.latitude, c.longitude]);
        const pl = new ymaps.Polyline(coords, {}, { strokeColor: p.color ?? '#2563eb', strokeWidth: p.width ?? 3, strokeStyle: p.dashed ? 'dash' : 'solid' });
        map.geoObjects.add(pl);
        polylineObjs[p.id] = pl;
    });

    (polygons ?? []).forEach(p => {
        const coords = [p.coordinates.map(c => [c.latitude, c.longitude])];
        const poly = new ymaps.Polygon(coords, {}, { fillColor: p.fillColor ?? '#2563eb26', strokeColor: p.strokeColor ?? '#2563eb', strokeWidth: p.strokeWidth ?? 2 });
        map.geoObjects.add(poly);
    });

    map.events.add('click', e => {
        const c = e.get('coords');
        try { dotnetRef.invokeMethodAsync('OnMapClickedAsync', { latitude: c[0], longitude: c[1] }); } catch {}
    });
    map.events.add('boundschange', () => {
        const c = map.getCenter();
        try { dotnetRef.invokeMethodAsync('OnViewChangedAsync', { centerLat: c[0], centerLon: c[1], zoom: map.getZoom() }); } catch {}
    });

    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { map.container.fitToViewport(); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { map, ymaps, markerObjs, polylineObjs, dotnetRef, ro, version: 'v21' });
}

// ── Public API ────────────────────────────────────────────────────────────────

export function setCenter(instanceId, lat, lon, zoom) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.version === 'v3') {
        inst.map.setLocation({ center: [lon, lat], zoom: zoom ?? inst.map.location.zoom, duration: 400 });
    } else {
        inst.map.setCenter([lat, lon], zoom ?? inst.map.getZoom(), { duration: 400 });
    }
}

export function setMapType(instanceId, mapType) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.version === 'v3') {
        try {
            inst.map.removeChild(inst.schemeLayer);
            inst.schemeLayer = new inst.ymaps3.YMapDefaultSchemeLayer({ customization: [] });
            inst.map.addChild(inst.schemeLayer);
        } catch {}
    } else {
        const t = { satellite: 'yandex#satellite', hybrid: 'yandex#hybrid', map: 'yandex#map' };
        try { inst.map.setType(t[mapType] ?? 'yandex#map'); } catch {}
    }
}

export function setTraffic(instanceId, show) {
    const inst = _instances.get(instanceId);
    if (!inst || inst.version !== 'v21') return;
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
    if (inst.version === 'v3') {
        const { YMapMarker } = inst.ymaps3;
        const el = document.createElement('div');
        const img = document.createElement('img');
        img.src = _makeMarkerCanvas(m.color, m.icon);
        img.style.cssText = 'width:36px;height:44px;display:block;cursor:pointer;';
        el.appendChild(img);
        el.addEventListener('click', () => {
            try { inst.dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
        });
        const marker = new YMapMarker({ coordinates: [m.longitude, m.latitude], anchor: [0.5, 1] }, el);
        inst.markersCollection.addChild(marker);
        inst.markerObjs.push({ marker, data: m });
    } else {
        const iconUrl = _makeMarkerCanvas(m.color, m.icon);
        const pm = new inst.ymaps.Placemark([m.latitude, m.longitude], { hintContent: m.title ?? '' }, { iconLayout: 'default#image', iconImageHref: iconUrl, iconImageSize: [36, 44], iconImageOffset: [-18, -44] });
        pm.events.add('click', () => {
            try { inst.dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
        });
        inst.map.geoObjects.add(pm);
        inst.markerObjs.push({ marker: pm, data: m });
    }
}

export function clearMarkers(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.version === 'v3') {
        inst.markerObjs.forEach(({ marker }) => { try { inst.markersCollection.removeChild(marker); } catch {} });
    } else {
        inst.markerObjs.forEach(({ marker }) => { try { inst.map.geoObjects.remove(marker); } catch {} });
    }
    inst.markerObjs.length = 0;
}

export function drawPolyline(instanceId, id, coords, style) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (!coords || coords.length < 2) return;

    if (inst.version === 'v3') {
        const { YMapFeature } = inst.ymaps3;
        if (inst.polylineObjs[id]) { try { inst.map.removeChild(inst.polylineObjs[id]); } catch {} delete inst.polylineObjs[id]; }
        const coordinates = coords.map(c => [c.longitude, c.latitude]);
        const feature = new YMapFeature({ id, geometry: { type: 'LineString', coordinates }, style: { stroke: [{ color: style?.color ?? '#2563eb', width: style?.width ?? 4 }] } });
        inst.map.addChild(feature);
        inst.polylineObjs[id] = feature;
        try {
            const lons = coordinates.map(c => c[0]), lats = coordinates.map(c => c[1]);
            inst.map.setLocation({ bounds: [[Math.min(...lons), Math.min(...lats)], [Math.max(...lons), Math.max(...lats)]], duration: 500 });
        } catch {}
    } else {
        if (inst.polylineObjs[id]) { try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {} delete inst.polylineObjs[id]; }
        const path = coords.map(c => [c.latitude, c.longitude]);
        const pl = new inst.ymaps.Polyline(path, {}, { strokeColor: style?.color ?? '#2563eb', strokeWidth: style?.width ?? 4, strokeStyle: style?.dashed ? 'dash' : 'solid' });
        inst.map.geoObjects.add(pl);
        inst.polylineObjs[id] = pl;
        try { const b = pl.geometry.getBounds(); if (b) inst.map.setBounds(b, { checkZoomRange: true, zoomMargin: 60, duration: 500 }); } catch {}
    }
}

export function clearPolyline(instanceId, id) {
    const inst = _instances.get(instanceId);
    if (!inst || !inst.polylineObjs[id]) return;
    if (inst.version === 'v3') { try { inst.map.removeChild(inst.polylineObjs[id]); } catch {} }
    else { try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {} }
    delete inst.polylineObjs[id];
}

export function fitBounds(instanceId, south, west, north, east) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.version === 'v3') { try { inst.map.setLocation({ bounds: [[west, south], [east, north]], duration: 400 }); } catch {} }
    else { try { inst.map.setBounds([[south, west], [north, east]], { checkZoomRange: true, zoomMargin: 60, duration: 400 }); } catch {} }
}

// ── Real routing ─────────────────────────────────────────────────────────────
// Strategy:
//   v2.1 (UUID key): ymaps.route() built-in → REST API fallback → straight line
//   v3 (new key):    REST API → straight line

export async function buildRoute(instanceId, fromLat, fromLon, toLat, toLon, apiKey, alternatives) {
    const inst = _instances.get(instanceId);
    if (!inst) return { ok: false, error: 'Map not initialized', routes: [] };

    _clearRouteFeatures(inst);

    // ── v2.1: use ymaps.route() — works with the same JS API key ──
    if (inst.version === 'v21' && inst.ymaps) {
        try {
            const result = await _callYmapsRoute(inst, fromLat, fromLon, toLat, toLon, alternatives);
            if (result.ok) return result;
            console.warn('ymaps.route() failed:', result.error);
        } catch (e) {
            console.warn('ymaps.route() exception:', e?.message ?? e);
        }
    }

    // ── REST API (works for both v3 and v2.1 if key has Router API access) ──
    if (apiKey) {
        try {
            const yr = await _callYandexRouter(fromLat, fromLon, toLat, toLon, apiKey);
            if (yr.ok) return _renderRoutes(inst, yr.routes, fromLat, fromLon, toLat, toLon);
            console.warn('Yandex Router REST failed:', yr.error);
            // Return error to user so they know what happened
            return { ok: false, error: yr.error, straight: true, selectedIndex: 0,
                routes: [{ distanceKm: _haversine(fromLat, fromLon, toLat, toLon), distanceText: null, durationMin: null, durationText: null, steps: [] }] };
        } catch (e) {
            console.warn('Yandex Router REST exception:', e?.message ?? e);
        }
    }

    // ── Straight line fallback ──
    drawPolyline(instanceId, '__route_0__', [
        { latitude: fromLat, longitude: fromLon },
        { latitude: toLat,   longitude: toLon   },
    ], { color: '#2563eb', width: 5, dashed: false });
    const dist = _haversine(fromLat, fromLon, toLat, toLon);
    return { ok: true, straight: true, selectedIndex: 0,
        routes: [{ distanceKm: dist, distanceText: `${dist.toFixed(1)} км`, durationMin: null, durationText: null, steps: [] }] };
}

// ── ymaps.route() for v2.1 ────────────────────────────────────────────────────

async function _callYmapsRoute(inst, fromLat, fromLon, toLat, toLon, alternatives) {
    const ymaps = inst.ymaps;

    let routeResult;
    try {
        routeResult = await ymaps.route(
            [[fromLat, fromLon], [toLat, toLon]],
            { routingMode: 'auto', results: alternatives ? 3 : 1 }
        );
    } catch (e) {
        return { ok: false, error: String(e?.message ?? e) };
    }

    // ymaps.route() can return an error object instead of throwing
    if (!routeResult) return { ok: false, error: 'ymaps.route() returned null' };

    // Check for error in result
    if (routeResult.getStatus && routeResult.getStatus() !== 'success') {
        return { ok: false, error: `ymaps.route status: ${routeResult.getStatus()}` };
    }

    // Check if route has paths
    let paths;
    try { paths = routeResult.getPaths(); } catch (e) { return { ok: false, error: String(e?.message ?? e) }; }

    if (!paths || (paths.getLength && paths.getLength() === 0)) {
        return { ok: false, error: 'Маршрут не найден' };
    }

    // Add to map
    inst.map.geoObjects.add(routeResult);
    if (!inst._routeFeatures) inst._routeFeatures = [];
    inst._routeFeatures.push(routeResult);

    // Style all paths
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

    // A/B markers
    _addWaypointMarker(inst, fromLon, fromLat, '#2563eb', 'A');
    _addWaypointMarker(inst, toLon,   toLat,   '#dc2626', 'B');

    // Collect route info
    const routes = [];
    const count = paths.getLength();
    for (let i = 0; i < count; i++) {
        const path = paths.get(i);
        let distM = 0, durSec = null;
        try { distM = path.getLength() ?? 0; } catch {}
        try { durSec = path.getDuration ? path.getDuration() : null; } catch {}

        // Extract turn-by-turn instructions from segments
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
}

// ── Yandex Router REST API v2 ─────────────────────────────────────────────────
// Docs: https://yandex.com/maps-api/docs/router-api/request.html
// waypoints format: lat,lon|lat,lon
// polyline.points format: [[lat, lon], ...]

async function _callYandexRouter(fromLat, fromLon, toLat, toLon, apiKey) {
    const url = `https://api.routing.yandex.net/v2/route` +
                `?apikey=${encodeURIComponent(apiKey)}` +
                `&waypoints=${fromLat},${fromLon}|${toLat},${toLon}` +
                `&mode=driving` +
                `&results=3`;

    const resp = await fetch(url);

    if (!resp.ok) {
        let errMsg = `HTTP ${resp.status}`;
        try { const e = await resp.json(); errMsg = e.errors?.[0] ?? errMsg; } catch {}
        return { ok: false, error: errMsg };
    }

    const data = await resp.json();

    // Handle both single route (data.route) and multiple routes (data.routes)
    const routeList = data.routes ?? (data.route ? [data.route] : []);
    if (!routeList.length) return { ok: false, error: 'Маршрут не найден' };

    const routes = routeList.map(route => {
        const legs = route.legs ?? [];
        let totalDist = 0, totalDur = 0;
        const allCoords = []; // [lon, lat] for rendering
        const steps = [];

        legs.forEach(leg => {
            (leg.steps ?? []).forEach(step => {
                totalDist += step.length ?? 0;
                totalDur  += step.duration ?? 0;
                // points are [lat, lon] — convert to [lon, lat] for our renderer
                (step.polyline?.points ?? []).forEach(pt => allCoords.push([pt[1], pt[0]]));
                if (step.length > 0) {
                    steps.push({
                        instruction: `${step.mode ?? 'driving'}`,
                        distance:    _formatDist(step.length),
                        duration:    _formatDur(step.duration ?? 0),
                    });
                }
            });
        });

        if (allCoords.length < 2) return null;

        return {
            coords:       allCoords,
            bbox:         null,
            distanceKm:   totalDist / 1000,
            distanceText: _formatDist(totalDist),
            durationMin:  Math.round(totalDur / 60),
            durationText: _formatDur(totalDur),
            steps,
        };
    }).filter(Boolean);

    if (!routes.length) return { ok: false, error: 'Не удалось разобрать маршрут' };
    return { ok: true, routes };
}

// ── Render routes on map ──────────────────────────────────────────────────────

function _renderRoutes(inst, routes, fromLat, fromLon, toLat, toLon) {
    inst._routeFeatures = [];

    routes.forEach((route, idx) => {
        const isMain = idx === 0;
        const color  = isMain ? '#2563eb' : '#94a3b8';
        const width  = isMain ? 6 : 3;

        if (inst.version === 'v3') {
            const { YMapFeature } = inst.ymaps3;
            const feature = new YMapFeature({
                id: `__yroute_${idx}`,
                geometry: { type: 'LineString', coordinates: route.coords },
                style: { stroke: [{ color, width }], zIndex: isMain ? 10 : 5 },
            });
            inst.map.addChild(feature);
            inst._routeFeatures.push(feature);
        } else {
            const path = route.coords.map(c => [c[1], c[0]]); // [lat, lon] for v2.1
            const pl = new inst.ymaps.Polyline(path, {}, { strokeColor: color, strokeWidth: width });
            inst.map.geoObjects.add(pl);
            inst._routeFeatures.push(pl);
        }
    });

    // Fit to first route
    const first = routes[0];
    if (first.bbox) {
        const [minLon, minLat, maxLon, maxLat] = first.bbox;
        if (inst.version === 'v3') {
            try { inst.map.setLocation({ bounds: [[minLon, minLat], [maxLon, maxLat]], duration: 500 }); } catch {}
        } else {
            try { inst.map.setBounds([[minLat, minLon], [maxLat, maxLon]], { checkZoomRange: true, zoomMargin: 60, duration: 500 }); } catch {}
        }
    } else if (first.coords.length >= 2) {
        const lons = first.coords.map(c => c[0]), lats = first.coords.map(c => c[1]);
        if (inst.version === 'v3') {
            try { inst.map.setLocation({ bounds: [[Math.min(...lons), Math.min(...lats)], [Math.max(...lons), Math.max(...lats)]], duration: 500 }); } catch {}
        } else {
            try { inst.map.setBounds([[Math.min(...lats), Math.min(...lons)], [Math.max(...lats), Math.max(...lons)]], { checkZoomRange: true, zoomMargin: 60, duration: 500 }); } catch {}
        }
    }

    // A/B markers
    _addWaypointMarker(inst, fromLon, fromLat, '#2563eb', 'A');
    _addWaypointMarker(inst, toLon,   toLat,   '#dc2626', 'B');

    return {
        ok: true, straight: false, selectedIndex: 0,
        routes: routes.map(r => ({
            distanceKm:   r.distanceKm,
            distanceText: r.distanceText,
            durationMin:  r.durationMin,
            durationText: r.durationText,
            steps:        r.steps,
        })),
    };
}

export function selectRoute(instanceId, idx) {
    const inst = _instances.get(instanceId);
    if (!inst?._routeFeatures) return;
    inst._routeFeatures.forEach((f, i) => {
        const isMain = i === idx;
        const color  = isMain ? '#2563eb' : '#94a3b8';
        const width  = isMain ? 6 : 3;
        if (inst.version === 'v3') {
            try { f.update({ style: { stroke: [{ color, width }], zIndex: isMain ? 10 : 5 } }); } catch {}
        } else {
            try { f.options.set({ strokeColor: color, strokeWidth: width }); } catch {}
        }
    });
}

function _addWaypointMarker(inst, lon, lat, color, letter) {
    if (!inst._waypointMarkers) inst._waypointMarkers = [];
    const el = document.createElement('img');
    el.src = _makeWaypointCanvas(color, letter);
    el.style.cssText = 'width:28px;height:28px;display:block;';

    if (inst.version === 'v3') {
        const { YMapMarker } = inst.ymaps3;
        const marker = new YMapMarker({ coordinates: [lon, lat], anchor: [0.5, 0.5], zIndex: 20 }, el);
        inst.map.addChild(marker);
        inst._waypointMarkers.push({ v3: marker });
    } else {
        const pm = new inst.ymaps.Placemark([lat, lon], {},
            { iconLayout: 'default#image', iconImageHref: _makeWaypointCanvas(color, letter), iconImageSize: [28, 28], iconImageOffset: [-14, -14] });
        inst.map.geoObjects.add(pm);
        inst._waypointMarkers.push({ v21: pm });
    }
}

function _clearRouteFeatures(inst) {
    if (inst._routeFeatures) {
        inst._routeFeatures.forEach(f => {
            if (inst.version === 'v3') { try { inst.map.removeChild(f); } catch {} }
            else { try { inst.map.geoObjects.remove(f); } catch {} }
        });
        inst._routeFeatures = null;
    }
    if (inst._waypointMarkers) {
        inst._waypointMarkers.forEach(m => {
            if (m.v3) { try { inst.map.removeChild(m.v3); } catch {} }
            if (m.v21) { try { inst.map.geoObjects.remove(m.v21); } catch {} }
        });
        inst._waypointMarkers = null;
    }
    ['__route__', '__route_0__'].forEach(id => {
        if (inst.polylineObjs?.[id]) {
            if (inst.version === 'v3') { try { inst.map.removeChild(inst.polylineObjs[id]); } catch {} }
            else { try { inst.map.geoObjects.remove(inst.polylineObjs[id]); } catch {} }
            delete inst.polylineObjs[id];
        }
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

function _formatDist(m) {
    return m >= 1000 ? `${(m/1000).toFixed(1)} км` : `${Math.round(m)} м`;
}

function _formatDur(sec) {
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    return h > 0 ? `${h} ч ${m} мин` : `${m} мин`;
}

// ── Dispose ───────────────────────────────────────────────────────────────────

export function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    if (inst.version === 'v3') {
        try { inst.map.destroy(); } catch {}
    } else {
        try { inst.map.destroy(); } catch {}
    }
    _instances.delete(instanceId);
}
