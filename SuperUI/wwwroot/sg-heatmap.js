// SgHeatmap — Click analytics and heatmap overlay using heatmap.js

let _heatmapInstance = null;
let _clicks = [];
let _isTracking = false;
let _dotnetRef = null;
let _container = null;
let _overlay = null;

const HEATMAP_JS_URL = 'https://cdnjs.cloudflare.com/ajax/libs/heatmap.js/2.0.2/heatmap.min.js';

async function _ensureHeatmapJs() {
    if (window.h337) return window.h337;
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = HEATMAP_JS_URL;
        script.async = true;
        script.onload = () => resolve(window.h337);
        script.onerror = () => reject(new Error('Failed to load heatmap.js'));
        document.head.appendChild(script);
    });
}

export async function init(dotnetRef) {
    _dotnetRef = dotnetRef;
    window.addEventListener('click', _handleGlobalClick);
    console.log('[SgHeatmap] Tracker initialized');
}

function _handleGlobalClick(e) {
    if (!_isTracking) return;
    
    // Ignore clicks on the heatmap overlay itself
    if (_overlay && _overlay.contains(e.target)) return;

    // We want coordinates relative to the scrollable content
    // Find the main scrollable container if possible, otherwise use body
    const container = document.querySelector('.sui-content') || document.body;
    const rect = container.getBoundingClientRect();
    
    const clickData = {
        x: Math.round(e.clientX - rect.left + container.scrollLeft),
        y: Math.round(e.clientY - rect.top + container.scrollTop),
        value: 1,
        timestamp: Date.now(),
        path: window.location.pathname,
        element: e.target.tagName + (e.target.id ? '#' + e.target.id : '')
    };

    _clicks.push(clickData);
    
    if (_clicks.length >= 5) {
        _flushClicks();
    }
}

function _flushClicks() {
    if (_dotnetRef && _clicks.length > 0) {
        _dotnetRef.invokeMethodAsync('SaveClicks', _clicks);
        _clicks = [];
    }
}

export function startTracking() {
    _isTracking = true;
    console.log('[SgHeatmap] Tracking started');
}

export function stopTracking() {
    _isTracking = false;
    _flushClicks();
    console.log('[SgHeatmap] Tracking stopped');
}

export async function showHeatmap(data) {
    await _ensureHeatmapJs();
    
    if (_overlay) hideHeatmap();

    // Attach overlay to the scrollable container for perfect alignment
    const parent = document.querySelector('.sui-content') || document.body;
    
    _overlay = document.createElement('div');
    _overlay.className = 'sg-heatmap-overlay';
    _overlay.style.position = 'absolute';
    _overlay.style.top = '0';
    _overlay.style.left = '0';
    _overlay.style.width = parent.scrollWidth + 'px';
    _overlay.style.height = parent.scrollHeight + 'px';
    _overlay.style.zIndex = '99999';
    _overlay.style.pointerEvents = 'none';
    _overlay.id = 'sg-heatmap-overlay';
    
    // Ensure parent is positioned
    if (window.getComputedStyle(parent).position === 'static') {
        parent.style.position = 'relative';
    }
    
    parent.appendChild(_overlay);

    _heatmapInstance = window.h337.create({
        container: _overlay,
        radius: 40,
        maxOpacity: 0.6,
        minOpacity: 0,
        blur: 0.75
    });

    _heatmapInstance.setData({
        max: 10,
        data: data || []
    });

    console.log('[SgHeatmap] Heatmap overlay shown on', parent.className, 'with', data.length, 'points');
}

export function hideHeatmap() {
    if (_overlay) {
        if (_overlay.parentNode) {
            _overlay.parentNode.removeChild(_overlay);
        }
        _overlay = null;
        _heatmapInstance = null;
    }
    console.log('[SgHeatmap] Heatmap overlay hidden');
}

export function dispose() {
    window.removeEventListener('click', _handleGlobalClick);
    hideHeatmap();
    _flushClicks();
}
