// SgVehicleTracker — Smooth GPS animation for transport monitoring
// Uses Turf.js for geo-calculations and requestAnimationFrame for smooth interpolation

const _instances = new Map();
const TURF_URL = 'https://cdn.jsdelivr.net/npm/@turf/turf@6/turf.min.js';

function _esc(v) {
    if (v == null) return '';
    return String(v)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

async function _ensureTurf() {
    if (window.turf) return window.turf;
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = TURF_URL;
        script.async = true;
        script.onload = () => resolve(window.turf);
        script.onerror = () => reject(new Error(`Failed to load script: ${TURF_URL}`));
        document.head.appendChild(script);
    });
}

export async function init(containerId, initialLat, initialLon, zoom) {
    await _ensureTurf();

    if (!window.L) {
        console.error('[SgVehicleTracker] Leaflet (L) not found. Make sure leaflet.js is loaded.');
        return;
    }

    const map = L.map(containerId).setView([initialLat, initialLon], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    const vehicles = new Map();
    const animFrame = { id: null };
    _startAnimationLoop(vehicles, animFrame);
    _instances.set(containerId, { map, vehicles, animationFrameId: animFrame });
}

export function updateVehicle(containerId, id, lat, lon, durationMs, label, iconUrl) {
    const inst = _instances.get(containerId);
    if (!inst) return;

    const turf = window.turf;
    if (!turf) return;

    const { vehicles, map } = inst;
    let v = vehicles.get(id);
    const newPos = [lon, lat];

    if (!v) {
        const icon = L.divIcon({
            className: 'sg-vehicle-icon',
            html: `<div class="sg-vehicle-container" id="vehicle-${_esc(id)}">
                     <img src="${_esc(iconUrl || 'https://cdn-icons-png.flaticon.com/512/3202/3202926.png')}" style="width: 32px; height: 32px; transition: transform 0.2s;"/>
                     <div class="sg-vehicle-label">${_esc(label || id)}</div>
                   </div>`,
            iconSize: [32, 32],
            iconAnchor: [16, 16]
        });

        const marker = L.marker([lat, lon], { icon }).addTo(map);
        v = {
            id,
            marker,
            currentPos: newPos,
            targetPos: newPos,
            startTime: performance.now(),
            duration: durationMs,
            bearing: 0
        };
        vehicles.set(id, v);
    } else {
        v.startTime = performance.now();
        v.duration = durationMs;
        v.targetPos = newPos;

        const from = turf.point(v.currentPos);
        const to = turf.point(v.targetPos);
        v.bearing = turf.bearing(from, to);
    }
}

function _startAnimationLoop(vehicles, animFrame) {
    const turf = window.turf;

    const animate = (time) => {
        vehicles.forEach((v) => {
            const elapsed = time - v.startTime;
            const progress = Math.min(elapsed / v.duration, 1);

            if (progress < 1) {
                const from = turf.point(v.currentPos);
                const to = turf.point(v.targetPos);
                const distance = turf.distance(from, to);

                if (distance > 0) {
                    const interpolated = turf.along(turf.lineString([v.currentPos, v.targetPos]), distance * progress);
                    const coords = interpolated.geometry.coordinates;
                    v.marker.setLatLng([coords[1], coords[0]]);

                    const img = document.querySelector(`#vehicle-${CSS.escape(v.id)} img`);
                    if (img) {
                        img.style.transform = `rotate(${v.bearing}deg)`;
                    }
                }
            } else {
                v.currentPos = v.targetPos;
                v.marker.setLatLng([v.targetPos[1], v.targetPos[0]]);
            }
        });

        animFrame.id = requestAnimationFrame(animate);
    };

    animFrame.id = requestAnimationFrame(animate);
}

export function dispose(containerId) {
    const inst = _instances.get(containerId);
    if (!inst) return;
    if (inst.animationFrameId?.id) cancelAnimationFrame(inst.animationFrameId.id);
    if (inst.map) {
        inst.map.remove();
    }
    inst.vehicles.clear();
    _instances.delete(containerId);
}
