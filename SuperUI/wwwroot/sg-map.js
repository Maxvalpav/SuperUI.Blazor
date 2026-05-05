// SgMap - OpenLayers Integration Module for SuperUI Blazor

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

async function _ensureOl(sources) {
    if (sources?.olCss) _loadCss(sources.olCss);
    if (sources?.olScript) await _loadScript(sources.olScript);
    let ol = window.ol;
    let n = 0;
    while (!ol && n++ < 80) { await new Promise(r => setTimeout(r, 100)); ol = window.ol; }
    if (!ol) throw new Error('OpenLayers not loaded');
    return ol;
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Tile source builder ───────────────────────────────────────────────────────

function _buildTileSource(ol, opts) {
    switch (opts.tileLayer) {
        case 'OpenStreetMap':
            return new ol.source.OSM();
        case 'StamenToner':
            return new ol.source.XYZ({ url: 'https://stamen-tiles.a.ssl.fastly.net/toner/{z}/{x}/{y}.png', attributions: 'Map tiles by Stamen Design' });
        case 'StamenWatercolor':
            return new ol.source.XYZ({ url: 'https://stamen-tiles.a.ssl.fastly.net/watercolor/{z}/{x}/{y}.jpg', attributions: 'Map tiles by Stamen Design' });
        case 'CartoPositron':
            return new ol.source.XYZ({ url: 'https://{a-d}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png', attributions: '© CartoDB' });
        case 'CartoDarkMatter':
            return new ol.source.XYZ({ url: 'https://{a-d}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png', attributions: '© CartoDB' });
        case 'Custom':
            return new ol.source.XYZ({ url: opts.customTileUrl ?? '' });
        default:
            return null;
    }
}

// ── Marker style ──────────────────────────────────────────────────────────────

function _markerStyle(ol, marker) {
    const accent = _cssVar('--sui-accent', '#006fee');
    const color  = marker.color ?? accent;
    const size   = marker.size  ?? 32;
    const icon   = marker.icon  ?? '';

    const canvas = document.createElement('canvas');
    canvas.width  = size;
    canvas.height = size + 8; // extra for pin tail
    const ctx = canvas.getContext('2d');

    // Pin body (circle)
    const r = size / 2 - 2;
    const cx = size / 2, cy = size / 2;
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,0.9)';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Pin tail
    ctx.beginPath();
    ctx.moveTo(cx - 5, cy + r - 2);
    ctx.lineTo(cx, cy + r + 8);
    ctx.lineTo(cx + 5, cy + r - 2);
    ctx.fillStyle = color;
    ctx.fill();

    // Icon / text
    if (icon) {
        ctx.font = `${Math.round(size * 0.45)}px sans-serif`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillStyle = '#ffffff';
        ctx.fillText(icon, cx, cy);
    }

    return new ol.style.Style({
        image: new ol.style.Icon({
            img: canvas,
            imgSize: [canvas.width, canvas.height],
            anchor: [0.5, 1],
        }),
    });
}

// ── Popup ─────────────────────────────────────────────────────────────────────

function _createPopup(container) {
    let popup = container.querySelector('.sg-map-popup');
    if (!popup) {
        popup = document.createElement('div');
        popup.className = 'sg-map-popup';
        popup.innerHTML = `
            <button class="sg-map-popup-close">✕</button>
            <div class="sg-map-popup-title"></div>
            <div class="sg-map-popup-desc"></div>
            <div class="sg-map-popup-coords"></div>
        `;
        container.appendChild(popup);
        popup.querySelector('.sg-map-popup-close').addEventListener('click', () => {
            popup.style.display = 'none';
        });
    }
    return popup;
}

function _showPopup(popup, pixel, title, desc, lon, lat) {
    popup.querySelector('.sg-map-popup-title').textContent = title ?? '';
    popup.querySelector('.sg-map-popup-desc').textContent  = desc  ?? '';
    popup.querySelector('.sg-map-popup-coords').textContent = `${lat.toFixed(5)}, ${lon.toFixed(5)}`;
    popup.style.display = 'block';
    popup.style.left = (pixel[0] + 12) + 'px';
    popup.style.top  = (pixel[1] - popup.offsetHeight / 2) + 'px';
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initMap(dotnetRef, containerRef, instanceId, opts, markers, polylines, polygons, sources) {
    await disposeMap(instanceId);

    const ol = await _ensureOl(sources);

    // ── View ──
    const view = new ol.View({
        center: ol.proj.fromLonLat([opts.centerLon ?? 37.618, opts.centerLat ?? 55.751]),
        zoom:    opts.zoom    ?? 10,
        minZoom: opts.minZoom ?? 2,
        maxZoom: opts.maxZoom ?? 19,
    });

    // ── Layers ──
    const layers = [];
    const tileSource = _buildTileSource(ol, opts);
    if (tileSource) layers.push(new ol.layer.Tile({ source: tileSource }));

    // ── Marker vector layer ──
    const markerFeatures = (markers ?? []).map(m => {
        const f = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat([m.longitude, m.latitude])),
        });
        f.setId(m.id);
        f.set('_marker', m);
        f.setStyle(_markerStyle(ol, m));
        return f;
    });
    const markerSource = new ol.source.Vector({ features: markerFeatures });
    const markerLayer  = new ol.layer.Vector({ source: markerSource, zIndex: 10 });
    layers.push(markerLayer);

    // ── Polyline vector layer ──
    const lineFeatures = (polylines ?? []).map(p => {
        const coords = p.coordinates.map(c => ol.proj.fromLonLat([c.longitude, c.latitude]));
        const f = new ol.Feature({ geometry: new ol.geom.LineString(coords) });
        f.setId(p.id);
        f.setStyle(new ol.style.Style({
            stroke: new ol.style.Stroke({
                color: p.color ?? '#2563eb',
                width: p.width ?? 3,
                lineDash: p.dashed ? [8, 6] : undefined,
            }),
        }));
        return f;
    });
    const lineSource = new ol.source.Vector({ features: lineFeatures });
    const lineLayer  = new ol.layer.Vector({ source: lineSource, zIndex: 5 });
    layers.push(lineLayer);

    // ── Polygon vector layer ──
    const polyFeatures = (polygons ?? []).map(p => {
        const coords = [p.coordinates.map(c => ol.proj.fromLonLat([c.longitude, c.latitude]))];
        const f = new ol.Feature({ geometry: new ol.geom.Polygon(coords) });
        f.setId(p.id);
        f.set('_polygon', p);
        f.setStyle(new ol.style.Style({
            fill:   new ol.style.Fill({ color: p.fillColor ?? 'rgba(37,99,235,0.2)' }),
            stroke: new ol.style.Stroke({ color: p.strokeColor ?? '#2563eb', width: p.strokeWidth ?? 2 }),
        }));
        return f;
    });
    const polySource = new ol.source.Vector({ features: polyFeatures });
    const polyLayer  = new ol.layer.Vector({ source: polySource, zIndex: 4 });
    layers.push(polyLayer);

    // ── Controls ──
    const controls = [];
    if (opts.showAttribution !== false) controls.push(new ol.control.Attribution({ collapsible: true }));
    if (opts.showZoomControl  !== false) controls.push(new ol.control.Zoom());
    if (opts.showScaleLine    !== false) controls.push(new ol.control.ScaleLine());

    // ── Map ──
    // ol.interaction.defaults was removed in OL v10 — build the collection manually
    const _mwzEnabled = opts.mouseWheelZoom !== false;
    let interactions;
    if (typeof ol.interaction.defaults === 'function') {
        // OL v6–v9 compat
        interactions = ol.interaction.defaults({ mouseWheelZoom: _mwzEnabled });
    } else {
        // OL v10+: construct each interaction explicitly
        const iList = [
            new ol.interaction.DragPan(),
            new ol.interaction.DoubleClickZoom(),
            new ol.interaction.KeyboardPan(),
            new ol.interaction.KeyboardZoom(),
            new ol.interaction.DragRotate(),
            new ol.interaction.PinchZoom(),
            new ol.interaction.PinchRotate(),
        ];
        if (_mwzEnabled) iList.push(new ol.interaction.MouseWheelZoom());
        interactions = new ol.Collection(iList);
    }

    const map = new ol.Map({
        target: containerRef,
        layers,
        view,
        controls: new ol.Collection(controls),
        interactions,
    });

    // ── Popup ──
    const popup = _createPopup(containerRef);

    // ── Click handler ──
    map.on('click', (e) => {
        const features = map.getFeaturesAtPixel(e.pixel, { hitTolerance: 8 });
        const markerFeat = features?.find(f => f.get('_marker'));

        if (markerFeat) {
            const m = markerFeat.get('_marker');
            if (opts.showPopup !== false) {
                _showPopup(popup, e.pixel, m.title, m.description, m.longitude, m.latitude);
            }
            try {
                dotnetRef.invokeMethodAsync('OnMarkerClickedAsync', {
                    markerId:    String(m.id),
                    title:       m.title       ?? null,
                    description: m.description ?? null,
                    longitude:   m.longitude,
                    latitude:    m.latitude,
                    data:        m.data        ?? null,
                });
            } catch {}
        } else {
            popup.style.display = 'none';
            const lonLat = ol.proj.toLonLat(e.coordinate);
            try {
                dotnetRef.invokeMethodAsync('OnMapClickedAsync', {
                    longitude: lonLat[0],
                    latitude:  lonLat[1],
                });
            } catch {}
        }
    });

    // ── Hover cursor ──
    map.on('pointermove', (e) => {
        const hit = map.hasFeatureAtPixel(e.pixel, { hitTolerance: 8 });
        map.getTargetElement().style.cursor = hit ? 'pointer' : '';
    });

    // ── View change ──
    view.on('change', () => {
        const center = ol.proj.toLonLat(view.getCenter());
        try {
            dotnetRef.invokeMethodAsync('OnViewChangedAsync', {
                centerLon: center[0],
                centerLat: center[1],
                zoom:      view.getZoom(),
            });
        } catch {}
    });

    // ── Fit to markers ──
    if (opts.fitToMarkers && markerFeatures.length > 0) {
        const extent = markerSource.getExtent();
        view.fit(extent, { padding: [60, 60, 60, 60], maxZoom: 15, duration: 400 });
    }

    // ── Resize observer ──
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { try { map.updateSize(); } catch {} });
        });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { map, view, markerSource, lineSource, polySource, ol, ro, dotnetRef, opts });
}

export function updateMarkers(instanceId, markers) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { ol, markerSource } = inst;
    markerSource.clear();
    (markers ?? []).forEach(m => {
        const f = new ol.Feature({ geometry: new ol.geom.Point(ol.proj.fromLonLat([m.longitude, m.latitude])) });
        f.setId(m.id);
        f.set('_marker', m);
        f.setStyle(_markerStyle(ol, m));
        markerSource.addFeature(f);
    });
}

export function setCenter(instanceId, lon, lat, zoom) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.view.animate({ center: inst.ol.proj.fromLonLat([lon, lat]), zoom, duration: 400 });
}

export function fitToMarkers(instanceId, padding) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const extent = inst.markerSource.getExtent();
    if (!inst.ol.extent.isEmpty(extent)) {
        inst.view.fit(extent, { padding: padding ?? [60,60,60,60], maxZoom: 15, duration: 400 });
    }
}

export function getCenter(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return null;
    const c = inst.ol.proj.toLonLat(inst.view.getCenter());
    return { longitude: c[0], latitude: c[1], zoom: inst.view.getZoom() };
}

export function exportPng(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.map.once('rendercomplete', () => {
        const canvas = inst.map.getTargetElement().querySelector('canvas');
        if (!canvas) return;
        const a = document.createElement('a');
        a.href = canvas.toDataURL('image/png');
        a.download = `map-${Date.now()}.png`;
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    });
    inst.map.renderSync();
}

export function drawRoute(instanceId, routeId, coordinates, style) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { ol, lineSource, dotnetRef } = inst;

    // Remove existing features with same routeId prefix
    [`${routeId}`, `${routeId}-start`, `${routeId}-end`].forEach(id => {
        const f = lineSource.getFeatureById(id);
        if (f) lineSource.removeFeature(f);
    });

    if (!coordinates || coordinates.length < 2) return;

    const coords = coordinates.map(c => ol.proj.fromLonLat([c.longitude, c.latitude]));
    const f = new ol.Feature({ geometry: new ol.geom.LineString(coords) });
    f.setId(routeId);

    const color  = style?.color  ?? '#2563eb';
    const width  = style?.width  ?? 4;
    const dashed = style?.dashed ?? false;
    const noMarkers = style?.noMarkers ?? false;
    const noFit     = style?.noFit     ?? false;
    const routeIndex = style?.routeIndex ?? null;

    f.set('_routeIndex', routeIndex);

    f.setStyle([
        new ol.style.Style({
            stroke: new ol.style.Stroke({ color: 'rgba(0,0,0,0.18)', width: width + 4 }),
        }),
        new ol.style.Style({
            stroke: new ol.style.Stroke({
                color,
                width,
                lineDash: dashed ? [10, 8] : undefined,
                lineCap: 'round',
                lineJoin: 'round',
            }),
        }),
    ]);

    lineSource.addFeature(f);

    if (!noMarkers) {
        const startPt = new ol.Feature({ geometry: new ol.geom.Point(coords[0]) });
        startPt.setId(`${routeId}-start`);
        startPt.setStyle(_waypointStyle(ol, color, 'A'));
        lineSource.addFeature(startPt);

        const endPt = new ol.Feature({ geometry: new ol.geom.Point(coords[coords.length - 1]) });
        endPt.setId(`${routeId}-end`);
        endPt.setStyle(_waypointStyle(ol, color, 'B'));
        lineSource.addFeature(endPt);
    }

    if (!noFit) {
        const extent = f.getGeometry().getExtent();
        inst.view.fit(extent, { padding: [60, 60, 60, 60], maxZoom: 15, duration: 500 });
    }
}

// Highlight a specific route by index — dims all others
export function highlightRoute(instanceId, selectedIndex, routePrefix) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { ol, lineSource } = inst;
    const prefix = routePrefix ?? 'gh-route-';

    lineSource.getFeatures().forEach(f => {
        const idx = f.get('_routeIndex');
        if (idx === null || idx === undefined) return;
        const isMain = idx === selectedIndex;
        const color  = isMain ? '#2563eb' : '#cbd5e1';
        const width  = isMain ? 6 : 3;
        const dashed = !isMain;
        f.setStyle([
            new ol.style.Style({
                stroke: new ol.style.Stroke({ color: isMain ? 'rgba(0,0,0,0.18)' : 'transparent', width: width + 4 }),
            }),
            new ol.style.Style({
                stroke: new ol.style.Stroke({
                    color,
                    width,
                    lineDash: dashed ? [10, 8] : undefined,
                    lineCap: 'round',
                    lineJoin: 'round',
                }),
            }),
        ]);
        // Bring selected to front
        f.set('_zIndex', isMain ? 10 : 1);
    });

    // Fit to selected route
    const mainId = `${prefix}${selectedIndex}`;
    const mainF  = lineSource.getFeatureById(mainId);
    if (mainF) {
        const extent = mainF.getGeometry().getExtent();
        inst.view.fit(extent, { padding: [60, 60, 60, 60], maxZoom: 15, duration: 400 });
    }
}

// Fit view to a bounding box [minLon, minLat, maxLon, maxLat]
export function fitToBbox(instanceId, minLon, minLat, maxLon, maxLat) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { ol, view } = inst;
    const extent = ol.proj.transformExtent([minLon, minLat, maxLon, maxLat], 'EPSG:4326', 'EPSG:3857');
    view.fit(extent, { padding: [60, 60, 60, 60], maxZoom: 15, duration: 500 });
}

function _waypointStyle(ol, color, letter) {
    const size = 28;
    const cv = document.createElement('canvas');
    cv.width = size; cv.height = size;
    const ctx = cv.getContext('2d');
    ctx.beginPath();
    ctx.arc(size/2, size/2, size/2 - 2, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${size * 0.45}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(letter, size/2, size/2);
    return new ol.style.Style({
        image: new ol.style.Icon({ img: cv, imgSize: [size, size], anchor: [0.5, 0.5] }),
    });
}

export function clearRoute(instanceId, routeId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { lineSource } = inst;
    [`${routeId}`, `${routeId}-start`, `${routeId}-end`].forEach(id => {
        const f = lineSource.getFeatureById(id);
        if (f) lineSource.removeFeature(f);
    });
}

export async function disposeMap(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.map.setTarget(null); inst.map.dispose(); } catch {}
    _instances.delete(instanceId);
}
