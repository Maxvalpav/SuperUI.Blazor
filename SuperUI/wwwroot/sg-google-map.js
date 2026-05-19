// SgGoogleMap — Google Maps JavaScript API wrapper for SuperUI Blazor

const _instances = new Map();
let   _gmapsLoading = false;
let   _gmapsReady   = false;
let   _gmapsLoadedKey = null;   // track which key was used to load
const _gmapsQueue   = [];

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadGoogleMaps(apiKey) {
    const normalizedKey = apiKey || null;

    return new Promise((resolve, reject) => {
        // Already loaded with the same key (or both null) → reuse
        if (_gmapsReady && _gmapsLoadedKey === normalizedKey) { resolve(); return; }

        // Key changed — need to reload. Remove old script and reset state.
        if (_gmapsReady && _gmapsLoadedKey !== normalizedKey) {
            _gmapsReady   = false;
            _gmapsLoading = false;
            _gmapsLoadedKey = null;
            // Remove old Google Maps scripts
            document.querySelectorAll('script[src*="maps.googleapis.com"]').forEach(s => s.remove());
            // Remove old google object so new script initializes fresh
            delete window.google;
        }

        _gmapsQueue.push({ resolve, reject });
        if (_gmapsLoading) return;
        _gmapsLoading = true;

        window.__sgGmapsReady = () => {
            _gmapsReady     = true;
            _gmapsLoadedKey = normalizedKey;
            _gmapsQueue.forEach(p => p.resolve());
            _gmapsQueue.length = 0;
        };

        const s = document.createElement('script');
        const key = normalizedKey ? `&key=${normalizedKey}` : '';
        s.src = `https://maps.googleapis.com/maps/api/js?callback=__sgGmapsReady&libraries=geometry${key}`;
        s.async = true;
        s.defer = true;
        s.onerror = () => {
            _gmapsLoading = false;
            _gmapsQueue.forEach(p => p.reject(new Error('Failed to load Google Maps API')));
            _gmapsQueue.length = 0;
        };
        document.head.appendChild(s);
    });
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Marker icon ───────────────────────────────────────────────────────────────

function _makeMarkerIcon(color, emoji) {
    const accent = _cssVar('--sg-color-primary', '#006fee');
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

// ── Init ──────────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons) {
    await disposeMap(instanceId);
    await _loadGoogleMaps(opts.apiKey);

    const google = window.google;
    const mapOpts = {
        center:          { lat: opts.centerLat ?? 55.751, lng: opts.centerLon ?? 37.618 },
        zoom:            opts.zoom ?? 10,
        mapTypeId:       opts.mapTypeId ?? 'roadmap',
        disableDefaultUI: !opts.showControls,
        streetViewControl: opts.showStreetView ?? false,
        gestureHandling: opts.gestureHandling ?? 'auto',
    };

    if (opts.styles) {
        try { mapOpts.styles = JSON.parse(opts.styles); } catch {}
    }

    const map = new google.maps.Map(containerRef, mapOpts);

    // ── Markers ──
    const markerObjs = [];
    const infoWindow = new google.maps.InfoWindow();

    (markers ?? []).forEach(m => {
        const marker = new google.maps.Marker({
            position: { lat: m.latitude, lng: m.longitude },
            map,
            title: m.title ?? '',
            icon: { url: _makeMarkerIcon(m.color, m.icon), scaledSize: new google.maps.Size(36, 44), anchor: new google.maps.Point(18, 44) },
        });
        marker._data = m;
        marker.addListener('click', () => {
            infoWindow.setContent(`
                <div style="font-family:var(--sg-font,system-ui);padding:4px 2px;min-width:120px">
                    <div style="font-weight:600;font-size:13px;margin-bottom:2px">${m.title ?? ''}</div>
                    ${m.description ? `<div style="font-size:11px;color:#6b7280">${m.description}</div>` : ''}
                    <div style="font-size:10px;color:#9ca3af;margin-top:4px;font-family:monospace">${m.latitude.toFixed(5)}, ${m.longitude.toFixed(5)}</div>
                </div>`);
            infoWindow.open(map, marker);
            try { dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
        });
        markerObjs.push(marker);
    });

    // ── Polylines ──
    const polylineObjs = {};
    (polylines ?? []).forEach(p => {
        const path = p.coordinates.map(c => ({ lat: c.latitude, lng: c.longitude }));
        const pl = new google.maps.Polyline({
            path, map,
            strokeColor: p.color ?? '#2563eb',
            strokeWeight: p.width ?? 3,
            strokeOpacity: 1,
            icons: p.dashed ? [{ icon: { path: 'M 0,-1 0,1', strokeOpacity: 1, scale: 3 }, offset: '0', repeat: '16px' }] : [],
        });
        polylineObjs[p.id] = pl;
    });

    // ── Polygons ──
    (polygons ?? []).forEach(p => {
        const path = p.coordinates.map(c => ({ lat: c.latitude, lng: c.longitude }));
        new google.maps.Polygon({
            paths: path, map,
            fillColor: p.fillColor ?? 'rgba(37,99,235,0.2)',
            fillOpacity: 0.25,
            strokeColor: p.strokeColor ?? '#2563eb',
            strokeWeight: p.strokeWidth ?? 2,
        });
    });

    // ── Map click ──
    map.addListener('click', (e) => {
        infoWindow.close();
        try { dotnetRef.invokeMethodAsync('OnMapClickedAsync', { latitude: e.latLng.lat(), longitude: e.latLng.lng() }); } catch {}
    });

    // ── View change ──
    map.addListener('idle', () => {
        const c = map.getCenter();
        try { dotnetRef.invokeMethodAsync('OnViewChangedAsync', { centerLat: c.lat(), centerLon: c.lng(), zoom: map.getZoom() }); } catch {}
    });

    // ── Resize ──
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { google.maps.event.trigger(map, 'resize'); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { map, markerObjs, polylineObjs, infoWindow, google, dotnetRef, ro });
}

export function setCenter(instanceId, lat, lon, zoom) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.panTo({ lat, lng: lon });
    if (zoom != null) inst.map.setZoom(zoom);
}

export function setMapType(instanceId, mapTypeId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.setMapTypeId(mapTypeId);
}

export function addMarker(instanceId, m) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { map, markerObjs, infoWindow, google, dotnetRef } = inst;
    const marker = new google.maps.Marker({
        position: { lat: m.latitude, lng: m.longitude },
        map,
        title: m.title ?? '',
        icon: { url: _makeMarkerIcon(m.color, m.icon), scaledSize: new google.maps.Size(36, 44), anchor: new google.maps.Point(18, 44) },
    });
    marker._data = m;
    marker.addListener('click', () => {
        infoWindow.setContent(`<div style="font-weight:600">${m.title ?? ''}</div>`);
        infoWindow.open(map, marker);
        try { dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', { markerId: String(m.id), title: m.title ?? null, description: m.description ?? null, latitude: m.latitude, longitude: m.longitude, data: m.data ?? null }); } catch {}
    });
    markerObjs.push(marker);
}

export function clearMarkers(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.markerObjs.forEach(m => m.setMap(null));
    inst.markerObjs.length = 0;
}

export function drawPolyline(instanceId, id, coords, style) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { map, polylineObjs, google } = inst;
    if (polylineObjs[id]) { polylineObjs[id].setMap(null); delete polylineObjs[id]; }
    if (!coords || coords.length < 2) return;
    const path = coords.map(c => ({ lat: c.latitude, lng: c.longitude }));
    const pl = new google.maps.Polyline({
        path, map,
        strokeColor: style?.color ?? '#2563eb',
        strokeWeight: style?.width ?? 4,
        strokeOpacity: 1,
        icons: style?.dashed ? [{ icon: { path: 'M 0,-1 0,1', strokeOpacity: 1, scale: 3 }, offset: '0', repeat: '16px' }] : [],
    });
    polylineObjs[id] = pl;

    // Fit bounds
    const bounds = new google.maps.LatLngBounds();
    path.forEach(p => bounds.extend(p));
    inst.map.fitBounds(bounds, { top: 60, right: 60, bottom: 60, left: 60 });
}

export function clearPolyline(instanceId, id) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.polylineObjs[id]) { inst.polylineObjs[id].setMap(null); delete inst.polylineObjs[id]; }
}

export function fitBounds(instanceId, south, west, north, east) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const bounds = new inst.google.maps.LatLngBounds({ lat: south, lng: west }, { lat: north, lng: east });
    inst.map.fitBounds(bounds, { top: 60, right: 60, bottom: 60, left: 60 });
}

// ── Real routing ─────────────────────────────────────────────────────────────
// Strategy: 1) Google DirectionsService (requires Directions API enabled for key)
//           2) Straight line fallback

export async function buildRoute(instanceId, fromLat, fromLon, toLat, toLon, apiKey, alternatives) {
    const inst = _instances.get(instanceId);
    if (!inst) return { ok: false, error: 'Map not initialized', routes: [] };

    const { map } = inst;
    _clearRouteRenderers(inst);

    // ── Try Google DirectionsService if key provided ──
    if (apiKey) {
        if (_gmapsLoadedKey !== apiKey) {
            try { await _loadGoogleMaps(apiKey); inst.google = window.google; } catch {}
        }
        const google = inst.google ?? window.google;

        try {
            const result = await _callDirectionsService(google, fromLat, fromLon, toLat, toLon, alternatives);
            if (result.status === 'OK') {
                return _renderDirectionsResult(inst, result.data, google, map, instanceId, fromLat, fromLon, toLat, toLon);
            }
            console.warn('DirectionsService:', result.status);
        } catch {}
    }

    // ── Straight line fallback ──
    const coords = [{ latitude: fromLat, longitude: fromLon }, { latitude: toLat, longitude: toLon }];
    drawPolyline(instanceId, '__route_0__', coords, { color: '#2563eb', width: 5, dashed: false });
    const dist = _haversine(fromLat, fromLon, toLat, toLon);
    return { ok: true, straight: true, selectedIndex: 0,
        routes: [{ distanceKm: dist, distanceText: `${dist.toFixed(1)} км`, durationMin: null, durationText: null, steps: [] }] };
}

// ── Google DirectionsService ──────────────────────────────────────────────────

function _callDirectionsService(google, fromLat, fromLon, toLat, toLon, alternatives) {
    return new Promise((resolve) => {
        const service = new google.maps.DirectionsService();
        const request = {
            origin:      { lat: fromLat, lng: fromLon },
            destination: { lat: toLat,   lng: toLon   },
            travelMode:  google.maps.TravelMode.DRIVING,
            provideRouteAlternatives: !!alternatives,
            language: 'ru',
        };
        service.route(request, (data, status) => resolve({ status, data }));
    });
}

function _renderDirectionsResult(inst, result, google, map, instanceId, fromLat, fromLon, toLat, toLon) {
    const routes = [];
    inst._routeRenderers = [];

    result.routes.forEach((route, idx) => {
        const leg    = route.legs[0];
        const isMain = idx === 0;
        const renderer = new google.maps.DirectionsRenderer({
            map, directions: result, routeIndex: idx, suppressMarkers: true,
            polylineOptions: {
                strokeColor:   isMain ? '#2563eb' : '#94a3b8',
                strokeWeight:  isMain ? 6 : 3,
                strokeOpacity: isMain ? 1 : 0.65,
                zIndex:        isMain ? 10 : 5,
            },
        });
        inst._routeRenderers.push(renderer);
        routes.push({
            distanceKm:   leg.distance.value / 1000,
            distanceText: leg.distance.text,
            durationMin:  Math.round(leg.duration.value / 60),
            durationText: leg.duration.text,
            steps: leg.steps.map(s => ({
                instruction: (s.instructions ?? '').replace(/<[^>]+>/g, ''),
                distance:    s.distance?.text ?? '',
                duration:    s.duration?.text ?? '',
            })),
        });
    });

    const bounds = result.routes[0].bounds;
    if (bounds) map.fitBounds(bounds, { top: 60, right: 60, bottom: 60, left: 60 });
    _addWaypointMarker(google, map, fromLat, fromLon, '#2563eb', 'A');
    _addWaypointMarker(google, map, toLat,   toLon,   '#dc2626', 'B');
    return { ok: true, straight: false, selectedIndex: 0, routes };
}

function _addWaypointMarker(google, map, lat, lng, color, letter) {
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
    new google.maps.Marker({
        position: { lat, lng }, map,
        icon: { url: cv.toDataURL(), scaledSize: new google.maps.Size(size, size), anchor: new google.maps.Point(size/2, size/2) },
        zIndex: 20,
    });
}

export function selectRoute(instanceId, idx) {
    const inst = _instances.get(instanceId);
    if (!inst || !inst._routeRenderers) return;
    inst._routeRenderers.forEach((renderer, i) => {
        const isMain = i === idx;
        renderer.setOptions({
            polylineOptions: {
                strokeColor:   isMain ? '#2563eb' : '#94a3b8',
                strokeWeight:  isMain ? 6 : 3,
                strokeOpacity: isMain ? 1 : 0.65,
                zIndex:        isMain ? 10 : 5,
            },
        });
    });
}

function _clearRouteRenderers(inst) {
    if (inst._routeRenderers) {
        inst._routeRenderers.forEach(r => {
            try { r.setMap(null); } catch {}
        });
        inst._routeRenderers = null;
    }
    if (inst._routeRenderer) { try { inst._routeRenderer.setMap(null); } catch {} inst._routeRenderer = null; }
    if (inst.polylineObjs) {
        ['__route__', '__route_0__'].forEach(id => {
            if (inst.polylineObjs[id]) { inst.polylineObjs[id].setMap(null); delete inst.polylineObjs[id]; }
        });
    }
    // Remove waypoint markers added by buildRoute
    if (inst._waypointMarkers) {
        inst._waypointMarkers.forEach(m => m.setMap(null));
        inst._waypointMarkers = null;
    }
}

function _haversine(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2)**2 + Math.cos(lat1*Math.PI/180)*Math.cos(lat2*Math.PI/180)*Math.sin(dLon/2)**2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
}

export function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.markerObjs.forEach(m => m.setMap(null)); } catch {}
    try { Object.values(inst.polylineObjs).forEach(p => p.setMap(null)); } catch {}
    _instances.delete(instanceId);
}
