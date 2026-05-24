// SgVehicleTracker — Smooth GPS animation for transport monitoring
// Uses Turf.js for geo-calculations and requestAnimationFrame for smooth interpolation

let _map = null;
let _vehicles = new Map(); // id -> { marker, currentPos, targetPos, startTime, duration, bearing }
let _animationFrameId = null;

const TURF_URL = 'https://cdn.jsdelivr.net/npm/@turf/turf@6/turf.min.js';

async function _ensureTurf() {
    if (window.turf) return window.turf;
    return new Promise((resolve) => {
        const script = document.createElement('script');
        script.src = TURF_URL;
        script.async = true;
        script.onload = () => resolve(window.turf);
        document.head.appendChild(script);
    });
}

export async function init(containerId, initialLat, initialLon, zoom) {
    await _ensureTurf();
    
    // Check if Leaflet is available (standard in SuperUI)
    if (!window.L) {
        console.error('[SgVehicleTracker] Leaflet (L) not found. Make sure leaflet.js is loaded.');
        return;
    }

    _map = L.map(containerId).setView([initialLat, initialLon], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(_map);

    _startAnimationLoop();
}

export function updateVehicle(id, lat, lon, durationMs, label, iconUrl) {
    const turf = window.turf;
    if (!turf) return;

    let v = _vehicles.get(id);
    const newPos = [lon, lat]; // turf uses [lon, lat]

    if (!v) {
        // Create new vehicle
        const icon = L.divIcon({
            className: 'sg-vehicle-icon',
            html: `<div class="sg-vehicle-container" id="vehicle-${id}">
                     <img src="${iconUrl || 'https://cdn-icons-png.flaticon.com/512/3202/3202926.png'}" style="width: 32px; height: 32px; transition: transform 0.2s;"/>
                     <div class="sg-vehicle-label">${label || id}</div>
                   </div>`,
            iconSize: [32, 32],
            iconAnchor: [16, 16]
        });

        const marker = L.marker([lat, lon], { icon }).addTo(_map);
        v = {
            id,
            marker,
            currentPos: newPos,
            targetPos: newPos,
            startTime: performance.now(),
            duration: durationMs,
            bearing: 0
        };
        _vehicles.set(id, v);
    } else {
        // Update existing vehicle
        v.startTime = performance.now();
        v.duration = durationMs;
        v.targetPos = newPos;
        
        // Calculate bearing using turf
        const from = turf.point(v.currentPos);
        const to = turf.point(v.targetPos);
        v.bearing = turf.bearing(from, to);
    }
}

function _startAnimationLoop() {
    const turf = window.turf;
    
    const animate = (time) => {
        _vehicles.forEach((v) => {
            const elapsed = time - v.startTime;
            const progress = Math.min(elapsed / v.duration, 1);

            if (progress < 1) {
                // Interpolate position using turf
                const from = turf.point(v.currentPos);
                const to = turf.point(v.targetPos);
                const distance = turf.distance(from, to);
                
                if (distance > 0) {
                    const interpolated = turf.along(turf.lineString([v.currentPos, v.targetPos]), distance * progress);
                    const coords = interpolated.geometry.coordinates;
                    v.marker.setLatLng([coords[1], coords[0]]);
                    
                    // Update rotation
                    const img = document.querySelector(`#vehicle-${v.id} img`);
                    if (img) {
                        img.style.transform = `rotate(${v.bearing}deg)`;
                    }
                }
            } else {
                // Animation finished for this step
                v.currentPos = v.targetPos;
                v.marker.setLatLng([v.targetPos[1], v.targetPos[0]]);
            }
        });

        _animationFrameId = requestAnimationFrame(animate);
    };

    _animationFrameId = requestAnimationFrame(animate);
}

export function dispose() {
    if (_animationFrameId) cancelAnimationFrame(_animationFrameId);
    if (_map) {
        _map.remove();
        _map = null;
    }
    _vehicles.clear();
}
