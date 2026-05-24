// Sg2Gis — 2GIS MapGL wrapper for SuperUI Blazor

const _instances = new Map();
let _loading = false;
let _ready = false;
const _queue = [];

// ── Loader ────────────────────────────────────────────────────────────────────

async function _ensure2Gis() {
    if (_ready) return window.mapgl;
    if (_loading) {
        return new Promise(resolve => _queue.push(resolve));
    }

    _loading = true;
    const script = document.createElement('script');
    script.src = 'https://mapgl.2gis.com/api/js/v1';
    script.async = true;
    script.onload = () => {
        _ready = true;
        _loading = false;
        while (_queue.length) _queue.shift()(window.mapgl);
    };
    document.head.appendChild(script);

    return new Promise(resolve => _queue.push(resolve));
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons) {
    await disposeMap(instanceId);
    const mapgl = await _ensure2Gis();

    const map = new mapgl.Map(containerRef, {
        center: [opts.centerLon ?? 37.618, opts.centerLat ?? 55.751],
        zoom: opts.zoom ?? 13,
        rotation: opts.rotation ?? 0,
        pitch: opts.pitch ?? 0,
        key: opts.apiKey,
        style: opts.theme === 'dark' ? 'c08033c9-07f0-424a-b501-13f5d5e227a6' : undefined, // Example dark style or use default
        lang: opts.lang ?? 'ru',
    });

    const instance = {
        map,
        dotnetRef,
        markers: new Map(),
        polylines: new Map(),
        polygons: new Map(),
    };
    _instances.set(instanceId, instance);

    // ── Controls ──
    if (opts.showZoomControl !== false) {
        new mapgl.Control(map, '<div class="sg-2gis-zoom"></div>', { position: 'topRight' });
    }

    // ── Event listeners ──
    map.on('click', (e) => {
        if (e.target) return; // Ignore if clicked on marker/other object
        dotnetRef.invokeMethodAsync('HandleMapClick', {
            longitude: e.lngLat[0],
            latitude: e.lngLat[1]
        });
    });

    map.on('moveend', () => {
        const center = map.getCenter();
        dotnetRef.invokeMethodAsync('HandleViewChanged', {
            centerLon: center[0],
            centerLat: center[1],
            zoom: map.getZoom(),
            rotation: map.getRotation(),
            pitch: map.getPitch()
        });
    });

    // ── Initial data ──
    if (markers) updateMarkers(instanceId, markers);
    if (polylines) updatePolylines(instanceId, polylines);
    if (polygons) updatePolygons(instanceId, polygons);

    return true;
}

export function updateMarkers(instanceId, markers) {
    const inst = _instances.get(instanceId);
    if (!inst) return;

    // Remove old
    inst.markers.forEach(m => m.destroy());
    inst.markers.clear();

    const mapgl = window.mapgl;
    (markers ?? []).forEach(m => {
        const marker = new mapgl.Marker(inst.map, {
            coordinates: [m.longitude, m.latitude],
            label: m.title ? { text: m.title, offset: [0, -40] } : undefined,
        });
        marker.on('click', () => {
            inst.dotnetRef.invokeMethodAsync('HandleMarkerClick', {
                markerId: m.id,
                title: m.title,
                description: m.description,
                longitude: m.longitude,
                latitude: m.latitude,
                data: m.data
            });
        });
        inst.markers.set(m.id, marker);
    });
}

export function updatePolylines(instanceId, polylines) {
    const inst = _instances.get(instanceId);
    if (!inst) return;

    inst.polylines.forEach(p => p.destroy());
    inst.polylines.clear();

    const mapgl = window.mapgl;
    (polylines ?? []).forEach(p => {
        const polyline = new mapgl.Polyline(inst.map, {
            coordinates: p.coordinates.map(c => [c.longitude, c.latitude]),
            width: p.width ?? 3,
            color: p.color ?? '#2563eb',
        });
        inst.polylines.set(p.id, polyline);
    });
}

export function updatePolygons(instanceId, polygons) {
    const inst = _instances.get(instanceId);
    if (!inst) return;

    inst.polygons.forEach(p => p.destroy());
    inst.polygons.clear();

    const mapgl = window.mapgl;
    (polygons ?? []).forEach(p => {
        const polygon = new mapgl.Polygon(inst.map, {
            coordinates: [p.coordinates.map(c => [c.longitude, c.latitude])],
            color: p.fillColor ?? 'rgba(37,99,235,0.2)',
            outlineWidth: p.strokeWidth ?? 2,
            outlineColor: p.strokeColor ?? '#2563eb',
        });
        inst.polygons.set(p.id, polygon);
    });
}

export async function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (inst) {
        inst.markers.forEach(m => m.destroy());
        inst.polylines.forEach(p => p.destroy());
        inst.polygons.forEach(p => p.destroy());
        inst.map.destroy();
        _instances.delete(instanceId);
    }
}

export function setCenter(instanceId, lon, lat, zoom) {
    const inst = _instances.get(instanceId);
    if (inst) {
        inst.map.setCenter([lon, lat]);
        if (zoom !== undefined) inst.map.setZoom(zoom);
    }
}
