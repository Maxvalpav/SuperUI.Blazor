function _esc(v) {
    if (v == null) return '';
    return String(v)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

let maps = new Map();

export function initTracerouteMap(containerId, options) {
    if (maps.has(containerId)) {
        maps.get(containerId).remove();
    }

    const map = L.map(containerId).setView([20, 0], 2);
    
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    maps.set(containerId, {
        map: map,
        markers: [],
        polyline: null
    });
}

export function updateHops(containerId, hops) {
    const state = maps.get(containerId);
    if (!state) return;

    const { map } = state;

    // Clear old
    state.markers.forEach(m => map.removeLayer(m));
    if (state.polyline) map.removeLayer(state.polyline);
    state.markers = [];

    const latlngs = [];

    hops.forEach(hop => {
        if (hop.latitude === 0 && hop.longitude === 0) return;

        const pos = [hop.latitude, hop.longitude];
        latlngs.push(pos);

        // Color based on latency
        let color = '#52c41a'; // Green
        if (hop.latencyMs > 150) color = '#f5222d'; // Red
        else if (hop.latencyMs > 70) color = '#faad14'; // Orange

        const marker = L.circleMarker(pos, {
            radius: 6,
            fillColor: color,
            color: '#fff',
            weight: 2,
            opacity: 1,
            fillOpacity: 0.8
        }).addTo(map);

        marker.bindPopup(`
            <b>Hop ${_esc(hop.hopIndex)}: ${_esc(hop.ip)}</b><br/>
            Location: ${_esc(hop.city)}, ${_esc(hop.country)}<br/>
            ISP: ${_esc(hop.isp)}<br/>
            Latency: ${_esc(hop.latencyMs.toFixed(1))} ms
        `);

        state.markers.push(marker);
    });

    if (latlngs.length > 1) {
        state.polyline = L.polyline(latlngs, {
            color: '#1890ff',
            weight: 3,
            dashArray: '5, 10',
            opacity: 0.6
        }).addTo(map);

        // Zoom to fit path
        map.fitBounds(state.polyline.getBounds(), { padding: [50, 50] });
    }
}

export function dispose(containerId) {
    if (maps.has(containerId)) {
        maps.get(containerId).map.remove();
        maps.delete(containerId);
    }
}
