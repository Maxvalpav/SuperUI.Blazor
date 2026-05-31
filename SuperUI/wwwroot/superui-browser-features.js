// --- Support Check ---
export function checkApiSupport(apiName) {
    switch (apiName) {
        case 'FaceDetector': return !!window.FaceDetector;
        case 'TextDetector': return !!window.TextDetector;
        case 'BarcodeDetector': return !!window.BarcodeDetector;
        case 'EyeDropper': return !!window.EyeDropper;
        case 'USB': return !!navigator.usb;
        case 'Serial': return !!navigator.serial;
        case 'Bluetooth': return !!navigator.bluetooth;
        case 'WakeLock': return 'wakeLock' in navigator;
        case 'IdleDetector': return 'IdleDetector' in window;
        case 'Sanitizer': return !!window.Sanitizer;
        case 'Navigation': return !!window.navigation;
        case 'Ink': return !!navigator.ink;
        case 'FileSystem': return !!window.showOpenFilePicker;
        case 'ScreenCapture': return !!navigator.mediaDevices.getDisplayMedia;
        case 'NFC': return 'NDEFReader' in window;
        case 'Contacts': return 'contacts' in navigator && 'ContactsManager' in window;
        case 'Badging': return 'setAppBadge' in navigator;
        case 'Fonts': return 'queryLocalFonts' in window;
        case 'MultiScreen': return 'getScreenDetails' in window;
        case 'ComputePressure': return 'ComputePressureObserver' in window;
        case 'Payment': return 'PaymentRequest' in window;
        case 'VirtualKeyboard': return 'virtualKeyboard' in navigator;
        case 'MIDI': return !!navigator.requestMIDIAccess;
        case 'SpeculationRules': return HTMLScriptElement.supports && HTMLScriptElement.supports('speculationrules');
        default: return false;
    }
}

let activeDotNetRef = null;

export async function getBatteryStatus() {
    if (!navigator.getBattery) return null;
    const battery = await navigator.getBattery();
    return {
        level: battery.level * 100,
        charging: battery.charging,
        chargingTime: battery.chargingTime,
        dischargingTime: battery.dischargingTime
    };
}

export function getNetworkStatus() {
    const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
    if (!conn) return null;
    return {
        type: conn.effectiveType, // '4g', '3g', etc.
        downlink: conn.downlink,
        rtt: conn.rtt,
        saveData: conn.saveData
    };
}

export function getCurrentPosition() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject("Geolocation not supported");
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (pos) => resolve({
                latitude: pos.coords.latitude,
                longitude: pos.coords.longitude,
                accuracy: pos.coords.accuracy,
                altitude: pos.coords.altitude
            }),
            (err) => reject(err.message),
            { enableHighAccuracy: true, timeout: 5000, maximumAge: 0 }
        );
    });
}

export async function pickColor() {
    if (!window.EyeDropper) return null;
    const eyeDropper = new EyeDropper();
    try {
        const result = await eyeDropper.open();
        return result.sRGBHex;
    } catch (e) {
        return null;
    }
}

export function vibrate(pattern) {
    if (navigator.vibrate) {
        navigator.vibrate(pattern);
        return true;
    }
    return false;
}

// ── MediaQuery ──────────────────────────────────────────────────────────
let mqObserverRef = null;
let mqQueryList = null;

export function observeMediaQuery(query, dotNetRef) {
    mqObserverRef = dotNetRef;
    mqQueryList = window.matchMedia(query);
    const handler = (e) => dotNetRef.invokeMethodAsync('OnMatchChanged', e.matches);
    mqQueryList.addEventListener('change', handler);
    mqQueryList._handler = handler;
    dotNetRef.invokeMethodAsync('OnMatchChanged', mqQueryList.matches);
}

export function unobserveMediaQuery() {
    if (mqQueryList && mqQueryList._handler) {
        mqQueryList.removeEventListener('change', mqQueryList._handler);
        delete mqQueryList._handler;
    }
    mqQueryList = null;
    mqObserverRef = null;
}

// ── KeyboardShortcut ────────────────────────────────────────────────────
let shortcutRef = null;
let shortcutHandler = null;

export function registerShortcut(keys, dotNetRef) {
    unregisterShortcut();
    shortcutRef = dotNetRef;
    const parsed = keys.split('+').map(s => s.trim().toLowerCase());
    shortcutHandler = (e) => {
        const match = parsed.every(key => {
            if (key === 'ctrl') return e.ctrlKey || e.metaKey;
            if (key === 'shift') return e.shiftKey;
            if (key === 'alt') return e.altKey;
            if (key === 'meta') return e.metaKey;
            return e.key.toLowerCase() === key;
        });
        if (match) {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('OnShortcutExecuted');
        }
    };
    document.addEventListener('keydown', shortcutHandler);
}

export function unregisterShortcut() {
    if (shortcutHandler) {
        document.removeEventListener('keydown', shortcutHandler);
        shortcutHandler = null;
    }
    shortcutRef = null;
}

// ── BeforeUnload ────────────────────────────────────────────────────────
export function setBeforeUnload(prevent, message) {
    if (prevent) {
        window.onbeforeunload = () => message || true;
    } else {
        window.onbeforeunload = null;
    }
}

// ── VisibilitySensor (IntersectionObserver) ─────────────────────────────
const intersectionObservers = new Map();

export function observeIntersection(element, threshold, rootMargin, once, dotNetRef) {
    const cb = (entries) => {
        const entry = entries[0];
        dotNetRef.invokeMethodAsync('OnVisibilityChanged', entry.isIntersecting, entry.intersectionRatio);
        if (once && entry.isIntersecting) {
            unobserveIntersection(element);
        }
    };
    const observer = new IntersectionObserver(cb, {
        threshold: threshold ?? 0,
        rootMargin: rootMargin ?? '0px'
    });
    observer.observe(element);
    intersectionObservers.set(element, observer);
}

export function unobserveIntersection(element) {
    const observer = intersectionObservers.get(element);
    if (observer) {
        observer.disconnect();
        intersectionObservers.delete(element);
    }
}

// ── LocalStorage ────────────────────────────────────────────────────────
let localStorageRef = null;
let localStorageKey = null;
let localStorageHandler = null;

export function initLocalStorageWatcher(key, dotNetRef) {
    stopLocalStorageWatcher();
    localStorageRef = dotNetRef;
    localStorageKey = key;
    localStorageHandler = (e) => {
        if (e.key === key) {
            dotNetRef.invokeMethodAsync('OnStorageChanged', e.newValue || '');
        }
    };
    window.addEventListener('storage', localStorageHandler);
}

export function stopLocalStorageWatcher() {
    if (localStorageHandler) {
        window.removeEventListener('storage', localStorageHandler);
        localStorageHandler = null;
    }
    localStorageRef = null;
    localStorageKey = null;
}

// ── Fullscreen ──────────────────────────────────────────────────────────
let fullscreenRef = null;
let fullscreenHandler = null;

export function enterFullscreen(element) {
    if (element) {
        element.requestFullscreen();
    } else {
        document.documentElement.requestFullscreen();
    }
}

export function exitFullscreen() {
    if (document.fullscreenElement) {
        document.exitFullscreen();
    }
}

export function listenFullscreenChange(dotNetRef) {
    stopListeningFullscreen();
    fullscreenRef = dotNetRef;
    fullscreenHandler = () => {
        dotNetRef.invokeMethodAsync('OnFullscreenChanged', !!document.fullscreenElement);
    };
    document.addEventListener('fullscreenchange', fullscreenHandler);
}

export function stopListeningFullscreen() {
    if (fullscreenHandler) {
        document.removeEventListener('fullscreenchange', fullscreenHandler);
        fullscreenHandler = null;
    }
    fullscreenRef = null;
}

// ── FocusTracker ────────────────────────────────────────────────────────
let focusTrackerRef = null;
let focusTrackerScope = null;
let focusInHandler = null;
let focusOutHandler = null;

export function initFocusTracker(scope, dotNetRef) {
    stopFocusTracker();
    focusTrackerRef = dotNetRef;
    focusTrackerScope = scope;
    const root = scope ? document.querySelector(scope) : document;
    if (!root) return;
    focusInHandler = (e) => {
        const target = e.target;
        const selector = target.id ? '#' + target.id : target.tagName.toLowerCase() +
            (target.className ? '.' + target.className.split(' ').filter(c => c).join('.') : '');
        dotNetRef.invokeMethodAsync('OnFocusedIn', selector);
    };
    focusOutHandler = (e) => {
        const related = e.relatedTarget;
        if (!related || (scope && !related.closest(scope))) {
            dotNetRef.invokeMethodAsync('OnFocusedOut');
        }
    };
    root.addEventListener('focusin', focusInHandler);
    root.addEventListener('focusout', focusOutHandler);
}

export function stopFocusTracker() {
    const root = focusTrackerScope ? document.querySelector(focusTrackerScope) : document;
    if (root && focusInHandler) {
        root.removeEventListener('focusin', focusInHandler);
        root.removeEventListener('focusout', focusOutHandler);
    }
    focusInHandler = null;
    focusOutHandler = null;
    focusTrackerRef = null;
    focusTrackerScope = null;
}

export async function showNotification(title, options) {
    if (!("Notification" in window)) return false;
    if (Notification.permission === "granted") {
        new Notification(title, options);
        return true;
    } else if (Notification.permission !== "denied") {
        const permission = await Notification.requestPermission();
        if (permission === "granted") {
            new Notification(title, options);
            return true;
        }
    }
    return false;
}

export async function copyToClipboard(text) {
    if (!navigator.clipboard) return false;
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        return false;
    }
}

export async function readFromClipboard() {
    if (!navigator.clipboard) return null;
    try {
        return await navigator.clipboard.readText();
    } catch (err) {
        return null;
    }
}

export async function openFile() {
    if (!window.showOpenFilePicker) return null;
    try {
        const [fileHandle] = await window.showOpenFilePicker();
        const file = await fileHandle.getFile();
        const content = await file.text();
        return {
            name: file.name,
            size: file.size,
            type: file.type,
            content: content
        };
    } catch (err) {
        return null;
    }
}

export async function saveFile(content, fileName, type) {
    if (!window.showSaveFilePicker) return false;
    try {
        const handle = await window.showSaveFilePicker({
            suggestedName: fileName,
            types: [{
                description: 'Text file',
                accept: { [type || 'text/plain']: ['.txt', '.json', '.md'] },
            }],
        });
        const writable = await handle.createWritable();
        await writable.write(content);
        await writable.close();
        return true;
    } catch (err) {
        return false;
    }
}

// --- Bluetooth API ---
let bluetoothDevice = null;
let bluetoothServer = null;

export async function requestBluetoothDevice(options) {
    if (!navigator.bluetooth) return null;
    try {
        bluetoothDevice = await navigator.bluetooth.requestDevice(options || { acceptAllDevices: true });
        return {
            id: bluetoothDevice.id,
            name: bluetoothDevice.name
        };
    } catch (e) {
        return null;
    }
}

export async function connectBluetooth() {
    if (!bluetoothDevice) return false;
    try {
        bluetoothServer = await bluetoothDevice.gatt.connect();
        return true;
    } catch (e) {
        return false;
    }
}

export async function disconnectBluetooth() {
    if (bluetoothDevice && bluetoothDevice.gatt.connected) {
        bluetoothDevice.gatt.disconnect();
        return true;
    }
    return false;
}

// --- Speculation Rules ---
export function applySpeculationRules(rules) {
    let script = document.querySelector('script[type="speculationrules"]');
    if (script) {
        script.remove();
    }
    
    script = document.createElement('script');
    script.type = 'speculationrules';
    script.textContent = JSON.stringify(rules);
    document.head.appendChild(script);
}

// --- Compute Pressure API ---
let pressureObserver = null;

export async function observeComputePressure(dotNetRef) {
    if (!('ComputePressureObserver' in window)) return false;
    try {
        pressureObserver = new ComputePressureObserver(async (records) => {
            const lastRecord = records[records.length - 1];
            // Support both old and new event naming for compatibility
            await dotNetRef.invokeMethodAsync('OnPressureUpdate', lastRecord.state);
            await dotNetRef.invokeMethodAsync('HandlePressureChanged', records.map(r => ({
                source: r.source,
                state: r.state,
                factors: r.factors
            })));
        });
        await pressureObserver.observe('cpu');
        return true;
    } catch (e) {
        console.error("Compute Pressure failed", e);
        return false;
    }
}

export function unobserveComputePressure() {
    if (pressureObserver) {
        pressureObserver.disconnect();
        pressureObserver = null;
    }
}

export function stopComputePressure() {
    unobserveComputePressure();
}

// --- Serial API ---
let serialPort = null;
let serialReader = null;
let serialWriter = null;

export async function requestSerialPort() {
    if (!navigator.serial) return false;
    try {
        serialPort = await navigator.serial.requestPort();
        return true;
    } catch (e) {
        return false;
    }
}

export async function openSerialPort(options) {
    if (!serialPort) return false;
    try {
        await serialPort.open(options || { baudRate: 9600 });
        serialWriter = serialPort.writable.getWriter();
        return true;
    } catch (e) {
        return false;
    }
}

export async function writeSerial(data) {
    if (!serialWriter) return false;
    try {
        const encoder = new TextEncoder();
        await serialWriter.write(encoder.encode(data));
        return true;
    } catch (e) {
        return false;
    }
}

export async function closeSerialPort() {
    if (serialPort) {
        if (serialWriter) {
            serialWriter.releaseLock();
            serialWriter = null;
        }
        await serialPort.close();
        return true;
    }
    return false;
}

// --- Presentation API ---
let presentationRequest = null;
let presentationConnection = null;

export async function startPresentation(url) {
    if (!window.PresentationRequest) return false;
    try {
        presentationRequest = new PresentationRequest(url);
        presentationConnection = await presentationRequest.start();
        return true;
    } catch (e) {
        return false;
    }
}

export function terminatePresentation() {
    if (presentationConnection) {
        presentationConnection.terminate();
        presentationConnection = null;
        return true;
    }
    return false;
}

// --- Broadcast Channel API ---
const channels = new Map();

export function initBroadcastChannel(name, dotNetRef) {
    if (channels.has(name)) return;
    const channel = new BroadcastChannel(name);
    channel.onmessage = (event) => {
        dotNetRef.invokeMethodAsync('OnMessageReceived', event.data);
    };
    channels.set(name, channel);
}

export function postToBroadcastChannel(name, data) {
    const channel = channels.get(name);
    if (channel) {
        channel.postMessage(data);
        return true;
    }
    return false;
}

export function closeBroadcastChannel(name) {
    const channel = channels.get(name);
    if (channel) {
        channel.close();
        channels.delete(name);
    }
}

// --- Camera Helper ---
export async function setupCamera(videoElementId) {
    console.log(`SgBrowserFeatures: setupCamera for ${videoElementId}`);
    const video = document.getElementById(videoElementId);
    if (!video) return false;
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
        video.srcObject = stream;
        return true;
    } catch (e) {
        console.error(`SgBrowserFeatures: camera setup error`, e);
        return false;
    }
}

// --- Native Barcode Detection API ---
export async function detectBarcodes(videoElementId) {
    if (!window.BarcodeDetector) return null;
    const detector = new BarcodeDetector();
    const video = document.getElementById(videoElementId);
    if (!video) return null;
    try {
        const barcodes = await detector.detect(video);
        return barcodes.map(b => ({
            rawValue: b.rawValue,
            format: b.format
        }));
    } catch (e) {
        return null;
    }
}

// --- Web Audio API (Visualizer) ---
let audioCtx = null;
let analyser = null;
let source = null;
let animationId = null;

export async function startAudioVisualizer(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;
    const ctx = canvas.getContext('2d');

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        analyser = audioCtx.createAnalyser();
        source = audioCtx.createMediaStreamSource(stream);
        source.connect(analyser);
        analyser.fftSize = 256;

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        const draw = () => {
            animationId = requestAnimationFrame(draw);
            analyser.getByteFrequencyData(dataArray);

            ctx.fillStyle = 'rgba(0, 0, 0, 0.2)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            const barWidth = (canvas.width / bufferLength) * 2.5;
            let x = 0;

            for (let i = 0; i < bufferLength; i++) {
                const barHeight = (dataArray[i] / 255) * canvas.height;
                ctx.fillStyle = `rgb(${dataArray[i] + 100}, 50, 255)`;
                ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight);
                x += barWidth + 1;
            }
        };
        draw();
        return true;
    } catch (e) {
        return false;
    }
}

export function stopAudioVisualizer() {
    if (animationId) cancelAnimationFrame(animationId);
    if (audioCtx) audioCtx.close();
    audioCtx = null;
    analyser = null;
    source = null;
}

// --- Performance API ---
let lastFpsTime = 0;
let frames = 0;
let currentFps = 0;
let fpsRafId = 0;
let fpsSubscribers = 0;

function updateFps() {
    frames++;
    const now = performance.now();
    if (now >= lastFpsTime + 1000) {
        currentFps = Math.round((frames * 1000) / (now - lastFpsTime));
        frames = 0;
        lastFpsTime = now;
    }
    fpsRafId = requestAnimationFrame(updateFps);
}

function ensureFpsLoop() {
    if (fpsSubscribers === 0) {
        lastFpsTime = performance.now();
        frames = 0;
        fpsRafId = requestAnimationFrame(updateFps);
    }
    fpsSubscribers++;
}

function releaseFpsLoop() {
    if (fpsSubscribers > 0) fpsSubscribers--;
    if (fpsSubscribers === 0 && fpsRafId) {
        cancelAnimationFrame(fpsRafId);
        fpsRafId = 0;
        currentFps = 0;
    }
}

export function startPerformanceMonitoring() { ensureFpsLoop(); }
export function stopPerformanceMonitoring()  { releaseFpsLoop(); }

export function getPerformanceMetrics() {
    const metrics = {
        fps: currentFps,
        memory: null,
        timing: window.performance.timing
    };

    if (window.performance && window.performance.memory) {
        metrics.memory = {
            usedJSHeapSize: window.performance.memory.usedJSHeapSize,
            totalJSHeapSize: window.performance.memory.totalJSHeapSize,
            jsHeapSizeLimit: window.performance.memory.jsHeapSizeLimit
        };
    }
    return metrics;
}

export async function share(data) {
    if (navigator.share) {
        try {
            await navigator.share(data);
            return true;
        } catch (e) {
            return false;
        }
    }
    return false;
}

// --- WebUSB API ---
let usbDevice = null;

export async function requestUsbDevice(filters) {
    console.log("SgBrowserFeatures: requestUsbDevice called");
    if (!navigator.usb) {
        console.warn("SgBrowserFeatures: WebUSB not supported");
        return null;
    }
    try {
        usbDevice = await navigator.usb.requestDevice({ filters: filters || [] });
        return {
            vendorId: usbDevice.vendorId,
            productId: usbDevice.productId,
            productName: usbDevice.productName,
            manufacturerName: usbDevice.manufacturerName
        };
    } catch (e) {
        console.error("SgBrowserFeatures: WebUSB error", e);
        return null;
    }
}

// --- Screen Capture API ---
export async function startScreenCapture(videoElementId) {
    console.log(`SgBrowserFeatures: startScreenCapture for ${videoElementId}`);
    if (!navigator.mediaDevices.getDisplayMedia) {
        console.warn("SgBrowserFeatures: getDisplayMedia not supported");
        return false;
    }
    try {
        const stream = await navigator.mediaDevices.getDisplayMedia({ video: true });
        const video = document.getElementById(videoElementId);
        if (video) {
            video.srcObject = stream;
            return true;
        }
        return false;
    } catch (e) {
        console.error(`SgBrowserFeatures: screen capture error`, e);
        return false;
    }
}

// --- Picture-in-Picture API ---
export async function togglePip(videoElementId) {
    const video = document.getElementById(videoElementId);
    if (!video || !document.pictureInPictureEnabled) return false;
    try {
        if (document.pictureInPictureElement) {
            await document.exitPictureInPicture();
        } else {
            await video.requestPictureInPicture();
        }
        return true;
    } catch (e) {
        return false;
    }
}

// --- Shape Detection API (Face/Text) ---
export async function detectShapes(elementId, type) {
    console.log(`SgBrowserFeatures: detectShapes called for ${elementId} (${type})`);
    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`SgBrowserFeatures: element ${elementId} not found`);
        return null;
    }

    let detector;
    try {
        if (type === 'face' && window.FaceDetector) {
            detector = new FaceDetector();
        } else if (type === 'text' && window.TextDetector) {
            detector = new TextDetector();
        } else {
            console.warn(`SgBrowserFeatures: ${type} detector not supported in this browser`);
            return null;
        }

        const results = await detector.detect(element);
        console.log(`SgBrowserFeatures: detection results`, results);
        return results;
    } catch (e) {
        console.error(`SgBrowserFeatures: detection error`, e);
        return null;
    }
}

// --- Idle Detection API ---
let idleDetector = null;

export async function startIdleDetection(dotNetRef, threshold = 61000) {
    if (!('IdleDetector' in window)) return false;
    const state = await IdleDetector.requestPermission();
    if (state !== 'granted') return false;

    idleDetector = new IdleDetector();
    idleDetector.addEventListener('change', () => {
        dotNetRef.invokeMethodAsync('OnIdleStateChanged', {
            user: idleDetector.userState,
            screen: idleDetector.screenState
        });
    });

    await idleDetector.start({ threshold });
    return true;
}

// --- WebAuthn API (Passkeys) ---
export async function createPasskey(options) {
    if (!window.PublicKeyCredential) return null;
    // Simplification for demo: convert base64 strings to ArrayBuffers
    const challenge = Uint8Array.from(atob(options.challenge), c => c.charCodeAt(0));
    const user = {
        id: Uint8Array.from(atob(options.userId), c => c.charCodeAt(0)),
        name: options.userName,
        displayName: options.userDisplayName
    };
    
    const creationOptions = {
        publicKey: {
            challenge,
            rp: { name: options.rpName, id: window.location.hostname },
            user,
            pubKeyCredParams: [
                { alg: -7, type: "public-key" }, // ES256
                { alg: -257, type: "public-key" } // RS256
            ],
            authenticatorSelection: { userVerification: "preferred" },
            timeout: 60000
        }
    };

    try {
        const credential = await navigator.credentials.create(creationOptions);
        return {
            id: credential.id,
            type: credential.type,
            rawId: btoa(String.fromCharCode(...new Uint8Array(credential.rawId)))
        };
    } catch (e) {
        return null;
    }
}

// --- Web Locks API ---
export async function requestLock(name, dotNetRef) {
    if (!navigator.locks) return false;
    navigator.locks.request(name, async (lock) => {
        await dotNetRef.invokeMethodAsync('OnLockAcquired', name);
        // Keep the lock until signaled or component disposed
        return new Promise(resolve => {
            window[`_lockResolve_${name}`] = resolve;
        });
    });
    return true;
}

export function releaseLock(name) {
    if (window[`_lockResolve_${name}`]) {
        window[`_lockResolve_${name}`]();
        delete window[`_lockResolve_${name}`];
    }
}

// --- Compression Streams API ---
export async function compressData(data, format = 'gzip') {
    const stream = new Blob([data]).stream();
    const compressionStream = new CompressionStream(format);
    const compressedStream = stream.pipeThrough(compressionStream);
    const response = new Response(compressedStream);
    const blob = await response.blob();
    return new Uint8Array(await blob.arrayBuffer());
}

// --- Canvas Recorder API ---
let canvasRecorder = null;
let recordedChunks = [];

export function startCanvasRecording(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return false;
    
    recordedChunks = [];
    const stream = canvas.captureStream(30); // 30 FPS
    canvasRecorder = new MediaRecorder(stream, { mimeType: 'video/webm' });
    
    canvasRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) recordedChunks.push(e.data);
    };
    
    canvasRecorder.start();
    return true;
}

export async function stopCanvasRecording() {
    return new Promise((resolve) => {
        if (!canvasRecorder) return resolve(null);
        
        canvasRecorder.onstop = () => {
            const blob = new Blob(recordedChunks, { type: 'video/webm' });
            const url = URL.createObjectURL(blob);
            resolve(url);
        };
        canvasRecorder.stop();
    });
}

// --- Ink API (Low Latency) ---
let inkContexts = new Map();

export async function setupInk(canvasId, color = '#000', width = 2) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;
    
    // Adjust internal resolution to match display size
    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;
    
    let presenter = null;
    if (navigator.ink) {
        try {
            presenter = await navigator.ink.requestPresenter({ presentationArea: canvas });
        } catch (e) {
            console.warn("SgBrowserFeatures: Ink API presenter request failed", e);
        }
    }
    
    const ctx = canvas.getContext('2d');
    inkContexts.set(canvasId, { color, width, presenter });
    
    let isDrawing = false;
    
    const draw = (e) => {
        if (!isDrawing) return;
        const settings = inkContexts.get(canvasId);
        if (!settings) return;
        
        ctx.strokeStyle = settings.color;
        ctx.lineWidth = settings.width;
        ctx.lineJoin = 'round';
        ctx.lineCap = 'round';
        
        // Use ink presenter for low latency if available (only for pointer events that support it)
        if (settings.presenter && e.pointerType === 'pen') {
            try {
                settings.presenter.updateInkTray(e);
            } catch (err) { /* ignore */ }
        }
        
        ctx.lineTo(e.offsetX, e.offsetY);
        ctx.stroke();
        
        // Start a new path for the next segment to keep it smooth
        ctx.beginPath();
        ctx.moveTo(e.offsetX, e.offsetY);
    };

    canvas.onpointerdown = (e) => {
        isDrawing = true;
        canvas.setPointerCapture(e.pointerId);
        ctx.beginPath();
        ctx.moveTo(e.offsetX, e.offsetY);
    };
    
    canvas.onpointermove = draw;
    
    canvas.onpointerup = (e) => {
        isDrawing = false;
        canvas.releasePointerCapture(e.pointerId);
    };
    
    return true;
}

export function updateInkSettings(canvasId, color, width) {
    if (inkContexts.has(canvasId)) {
        const settings = inkContexts.get(canvasId);
        settings.color = color;
        settings.width = width;
    }
}

// --- Sanitizer API ---
export function sanitizeHtml(html) {
    // Check for the most modern API (setHTMLUnsafe) or fallback
    if (Element.prototype.setHTMLUnsafe) {
        const temp = document.createElement('div');
        temp.setHTMLUnsafe(html);
        return temp.innerHTML;
    }
    
    // Older Sanitizer API (deprecated but still in some browsers)
    if (window.Sanitizer) {
        try {
            const sanitizer = new Sanitizer();
            const temp = document.createElement('div');
            // Check if sanitizeToString exists, otherwise use standard sanitize
            if (sanitizer.sanitizeToString) {
                return sanitizer.sanitizeToString(html);
            } else {
                const fragment = sanitizer.sanitize(html);
                temp.appendChild(fragment);
                return temp.innerHTML;
            }
        } catch (e) {
            console.warn("Sanitizer API failed, using fallback", e);
        }
    }
    
    // Basic fallback for browsers without any Sanitizer API
    const temp = document.createElement('div');
    temp.textContent = html;
    return temp.innerHTML;
}

// --- Navigation API ---
export function initNavigation(dotNetRef) {
    if (!window.navigation) return false;
    
    navigation.addEventListener('navigate', (event) => {
        if (!event.canIntercept || event.hashChange || event.downloadRequest) return;
        
        const url = new URL(event.destination.url);
        
        event.intercept({
            async handler() {
                await dotNetRef.invokeMethodAsync('OnNavigateStarted', url.pathname);
                // The actual Blazor navigation will happen after this
            }
        });
    });
    
    return true;
}

// --- WebRTC API (Simple P2P) ---
let peerConnection = null;
const rtcConfig = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

export async function createOffer(dotNetRef) {
    peerConnection = new RTCPeerConnection(rtcConfig);
    
    peerConnection.onicecandidate = (event) => {
        if (event.candidate) {
            dotNetRef.invokeMethodAsync('OnIceCandidate', JSON.stringify(event.candidate));
        }
    };
    
    const dataChannel = peerConnection.createDataChannel("chat");
    dataChannel.onmessage = (e) => dotNetRef.invokeMethodAsync('OnP2PMessage', e.data);
    
    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    return JSON.stringify(offer);
}

// --- Presentation Receiver API ---
export function initPresentationReceiver(dotNetRef) {
    if (!navigator.presentation || !navigator.presentation.receiver) return false;
    
    navigator.presentation.receiver.connectionList.then(list => {
        list.connections.forEach(conn => {
            conn.onmessage = (e) => dotNetRef.invokeMethodAsync('OnPresentationMessage', e.data);
        });
        
        list.onconnectionavailable = (event) => {
            event.connection.onmessage = (e) => dotNetRef.invokeMethodAsync('OnPresentationMessage', e.data);
        };
    });
    return true;
}

// --- File Handling API ---
export function checkFileLaunch(dotNetRef) {
    if ('launchQueue' in window) {
        launchQueue.setConsumer(async (launchParams) => {
            if (!launchParams.files.length) return;
            for (const fileHandle of launchParams.files) {
                const file = await fileHandle.getFile();
                const content = await file.text();
                dotNetRef.invokeMethodAsync('OnFileLaunched', {
                    name: file.name,
                    content: content
                });
            }
        });
        return true;
    }
    return false;
}

export async function decompressData(compressedData, format = 'gzip') {
    const stream = new Blob([compressedData]).stream();
    const decompressionStream = new DecompressionStream(format);
    const decompressedStream = stream.pipeThrough(decompressionStream);
    const response = new Response(decompressedStream);
    const blob = await response.blob();
    return await blob.text();
}

// --- Web NFC API ---
let ndef = null;

export async function scanNfc(dotNetRef) {
    if (!('NDEFReader' in window)) return false;
    try {
        ndef = new NDEFReader();
        await ndef.scan();
        ndef.onreading = (event) => {
            const { serialNumber, message } = event;
            dotNetRef.invokeMethodAsync('OnNfcRead', {
                serialNumber,
                records: message.records.map(r => ({
                    recordType: r.recordType,
                    mediaType: r.mediaType,
                    data: new TextDecoder().decode(r.data)
                }))
            });
        };
        return true;
    } catch (e) {
        console.error("SgBrowserFeatures: NFC scan error", e);
        return false;
    }
}

export async function writeNfc(message) {
    if (!('NDEFReader' in window)) return false;
    try {
        const reader = new NDEFReader();
        await reader.write(message);
        return true;
    } catch (e) {
        console.error("SgBrowserFeatures: NFC write error", e);
        return false;
    }
}

// --- Contact Picker API ---
export async function pickContacts(properties, multiple = false) {
    if (!('contacts' in navigator)) return null;
    try {
        const contacts = await navigator.contacts.select(properties || ['name', 'email', 'tel'], { multiple });
        return contacts;
    } catch (e) {
        console.error("SgBrowserFeatures: Contact picker error", e);
        return null;
    }
}

// --- Badging API ---
export async function setAppBadge(value) {
    if ('setAppBadge' in navigator) {
        try {
            await navigator.setAppBadge(value);
            return true;
        } catch (e) {
            return false;
        }
    }
    return false;
}

export async function clearAppBadge() {
    if ('clearAppBadge' in navigator) {
        try {
            await navigator.clearAppBadge();
            return true;
        } catch (e) {
            return false;
        }
    }
    return false;
}

// --- Local Font Access API ---
export async function getLocalFonts() {
    if (!('queryLocalFonts' in window)) return null;
    try {
        const fonts = await window.queryLocalFonts();
        return fonts.map(f => ({
            family: f.family,
            fullName: f.fullName,
            postscriptName: f.postscriptName,
            style: f.style
        }));
    } catch (e) {
        return null;
    }
}

// --- Window Management API (Multi-Screen) ---
export async function getScreenDetails() {
    if (!('getScreenDetails' in window)) return null;
    try {
        const details = await window.getScreenDetails();
        return {
            screens: details.screens.map(s => ({
                label: s.label,
                isExtended: s.isExtended,
                isPrimary: s.isPrimary,
                isInternal: s.isInternal,
                width: s.width,
                height: s.height,
                left: s.left,
                top: s.top
            }))
        };
    } catch (e) {
        return null;
    }
}

// --- Payment Request API ---
export async function requestPayment(details, options) {
    if (!window.PaymentRequest) return null;
    const methods = [{ supportedMethods: 'basic-card' }];
    try {
        const request = new PaymentRequest(methods, details, options);
        const response = await request.show();
        await response.complete('success');
        return {
            methodName: response.methodName,
            details: response.details
        };
    } catch (e) {
        return null;
    }
}

// --- Virtual Keyboard API ---
export function setupVirtualKeyboard(dotNetRef) {
    if ('virtualKeyboard' in navigator) {
        navigator.virtualKeyboard.overlaysContent = true;
        navigator.virtualKeyboard.addEventListener('geometrychange', (event) => {
            const { x, y, width, height } = event.target.boundingRect;
            dotNetRef.invokeMethodAsync('OnKeyboardGeometryChanged', { x, y, width, height });
        });
        return true;
    }
    return false;
}

// --- Web MIDI API ---
let midiAccess = null;

export async function requestMidiAccess(dotNetRef) {
    if (!navigator.requestMIDIAccess) return false;
    try {
        midiAccess = await navigator.requestMIDIAccess();
        midiAccess.onstatechange = (e) => {
            dotNetRef.invokeMethodAsync('OnMidiStateChanged', {
                name: e.port.name,
                manufacturer: e.port.manufacturer,
                state: e.port.state,
                type: e.port.type
            });
        };
        return true;
    } catch (e) {
        return false;
    }
}

// ── ClickOutside ────────────────────────────────────────────────────────────
let clickOutsideRef = null;
let clickOutsideExclude = null;
let clickOutsideTarget = null;
let clickOutsideHandler = null;

export function initClickOutside(element, dotNetRef, excludeSelector) {
    disposeClickOutside();
    clickOutsideRef = dotNetRef;
    clickOutsideTarget = element;
    clickOutsideExclude = excludeSelector;
    clickOutsideHandler = (e) => {
        if (!dotNetRef) return;
        if (!element || element === e.target || element.contains(e.target)) return;
        if (excludeSelector && e.target.closest(excludeSelector)) return;
        dotNetRef.invokeMethodAsync('OnOutsideClick', e.clientX, e.clientY);
    };
    document.addEventListener('click', clickOutsideHandler, true);
}

export function setClickOutsideEnabled(enabled) {
    // The handler is handled in C# anyway, no JS-side changes needed
}

export function disposeClickOutside() {
    if (clickOutsideHandler) {
        document.removeEventListener('click', clickOutsideHandler, true);
        clickOutsideHandler = null;
    }
    clickOutsideRef = null;
    clickOutsideTarget = null;
    clickOutsideExclude = null;
}

// ── LongPress ────────────────────────────────────────────────────────────────
let longPressRef = null;
let longPressTarget = null;
let longPressDuration = 500;
let longPressTimer = null;
let longPressHandlers = null;

export function initLongPress(element, duration, dotNetRef) {
    disposeLongPress();
    longPressRef = dotNetRef;
    longPressTarget = element;
    longPressDuration = duration || 500;

    const start = (e) => {
        dotNetRef.invokeMethodAsync('OnPressStarted');
        longPressTimer = setTimeout(() => {
            dotNetRef.invokeMethodAsync('OnLongPressFired');
            longPressTimer = null;
        }, longPressDuration);
    };
    const end = () => {
        if (longPressTimer) {
            clearTimeout(longPressTimer);
            longPressTimer = null;
        }
        dotNetRef.invokeMethodAsync('OnPressEnded');
    };

    longPressHandlers = { start, end };
    element.addEventListener('mousedown', start);
    element.addEventListener('mouseup', end);
    element.addEventListener('mouseleave', end);
    element.addEventListener('touchstart', start, { passive: true });
    element.addEventListener('touchend', end, { passive: true });
    element.addEventListener('touchcancel', end, { passive: true });
}

export function disposeLongPress() {
    if (longPressTimer) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
    }
    if (longPressTarget && longPressHandlers) {
        longPressTarget.removeEventListener('mousedown', longPressHandlers.start);
        longPressTarget.removeEventListener('mouseup', longPressHandlers.end);
        longPressTarget.removeEventListener('mouseleave', longPressHandlers.end);
        longPressTarget.removeEventListener('touchstart', longPressHandlers.start);
        longPressTarget.removeEventListener('touchend', longPressHandlers.end);
        longPressTarget.removeEventListener('touchcancel', longPressHandlers.end);
    }
    longPressRef = null;
    longPressTarget = null;
    longPressHandlers = null;
}

// ── FileDrop ─────────────────────────────────────────────────────────────────
let fileDropRef = null;
let fileDropTarget = null;

export function initFileDrop(element, dotNetRef) {
    disposeFileDrop();
    fileDropRef = dotNetRef;
    fileDropTarget = element;

    element.addEventListener('dragenter', onFileDragEnter);
    element.addEventListener('dragover', onFileDragOver);
    element.addEventListener('dragleave', onFileDragLeave);
    element.addEventListener('drop', onFileDrop);
}

function onFileDragEnter(e) {
    e.preventDefault();
    if (fileDropRef) fileDropRef.invokeMethodAsync('OnDragEntered');
}

function onFileDragOver(e) {
    e.preventDefault();
}

function onFileDragLeave(e) {
    e.preventDefault();
    if (fileDropRef) fileDropRef.invokeMethodAsync('OnDragLeft');
}

function onFileDrop(e) {
    e.preventDefault();
    const files = e.dataTransfer?.files;
    if (!files || files.length === 0) return;
    const file = files[0];
    const reader = new FileReader();
    reader.onload = () => {
        if (fileDropRef) {
            fileDropRef.invokeMethodAsync('OnFileDropped', file.name, file.size, file.type, reader.result);
        }
    };
    reader.readAsDataURL(file);
}

export function disposeFileDrop() {
    if (fileDropTarget) {
        fileDropTarget.removeEventListener('dragenter', onFileDragEnter);
        fileDropTarget.removeEventListener('dragover', onFileDragOver);
        fileDropTarget.removeEventListener('dragleave', onFileDragLeave);
        fileDropTarget.removeEventListener('drop', onFileDrop);
    }
    fileDropRef = null;
    fileDropTarget = null;
}

// ── ElementSize (ResizeObserver) ─────────────────────────────────────────────
let elementSizeRef = null;
let elementSizeTarget = null;
let elementSizeObserver = null;

export function initElementSize(element, dotNetRef) {
    disposeElementSize();
    elementSizeRef = dotNetRef;
    elementSizeTarget = element;
    elementSizeObserver = new ResizeObserver((entries) => {
        const entry = entries[0];
        if (!entry) return;
        const { inlineSize, blockSize } = entry.borderBoxSize?.[0] || { inlineSize: entry.contentRect.width, blockSize: entry.contentRect.height };
        dotNetRef.invokeMethodAsync('OnSizeChangedInternal', inlineSize, blockSize);
    });
    elementSizeObserver.observe(element);
    // Fire initial size
    const rect = element.getBoundingClientRect();
    dotNetRef.invokeMethodAsync('OnSizeChangedInternal', rect.width, rect.height);
}

export function disposeElementSize() {
    if (elementSizeObserver) {
        elementSizeObserver.disconnect();
        elementSizeObserver = null;
    }
    elementSizeRef = null;
    elementSizeTarget = null;
}

// ── ScrollSpy (IntersectionObserver) ─────────────────────────────────────────
let scrollSpyRef = null;
let scrollSpyObserver = null;
let scrollSpySelector = '';

export function initScrollSpy(selector, rootMargin, dotNetRef) {
    disposeScrollSpy();
    scrollSpyRef = dotNetRef;
    scrollSpySelector = selector;

    const headings = document.querySelectorAll(selector);
    if (!headings.length) return;

    const cb = (entries) => {
        // Find the first intersecting entry at the top
        const visible = entries.filter(e => e.isIntersecting);
        if (visible.length > 0) {
            const top = visible.reduce((a, b) => a.boundingClientRect.top < b.boundingClientRect.top ? a : b);
            dotNetRef.invokeMethodAsync('OnActiveChangedInternal', top.target.id || null);
        }
    };

    scrollSpyObserver = new IntersectionObserver(cb, { rootMargin: rootMargin || '-80px 0px -80% 0px' });
    headings.forEach(h => scrollSpyObserver.observe(h));
}

export function disposeScrollSpy() {
    if (scrollSpyObserver) {
        scrollSpyObserver.disconnect();
        scrollSpyObserver = null;
    }
    scrollSpyRef = null;
}

// ── AutoFocus ────────────────────────────────────────────────────────────────
let focusDelayTimer = null;

export function focusElement(element) {
    if (!element) return;
    element.focus();
    if (element.select) element.select();
}

export function focusElementWithDelay(element, delayMs) {
    cancelFocusDelay();
    focusDelayTimer = setTimeout(() => {
        focusElement(element);
        focusDelayTimer = null;
    }, delayMs || 0);
}

export function cancelFocusDelay() {
    if (focusDelayTimer) {
        clearTimeout(focusDelayTimer);
        focusDelayTimer = null;
    }
}

// ── TextSelect ──────────────────────────────────────────────────────────────
let textSelectRef = null;
let textSelectScope = null;
let textSelectHandler = null;

export function initTextSelect(scope, dotNetRef) {
    disposeTextSelect();
    textSelectRef = dotNetRef;
    textSelectScope = scope;

    textSelectHandler = () => {
        const sel = window.getSelection();
        if (!sel || sel.isCollapsed || !sel.toString().trim()) {
            dotNetRef.invokeMethodAsync('OnTextSelected', null);
            return;
        }
        const text = sel.toString().trim();
        // If scope is set, verify the selection is within the scope
        if (scope) {
            const scopeEl = document.querySelector(scope);
            if (!scopeEl) return;
            let node = sel.anchorNode;
            while (node && node !== document) {
                if (node === scopeEl) {
                    dotNetRef.invokeMethodAsync('OnTextSelected', text);
                    return;
                }
                node = node.parentNode;
            }
            dotNetRef.invokeMethodAsync('OnTextSelected', null);
        } else {
            dotNetRef.invokeMethodAsync('OnTextSelected', text);
        }
    };

    document.addEventListener('mouseup', textSelectHandler);
    document.addEventListener('keyup', textSelectHandler);
}

export function disposeTextSelect() {
    if (textSelectHandler) {
        document.removeEventListener('mouseup', textSelectHandler);
        document.removeEventListener('keyup', textSelectHandler);
        textSelectHandler = null;
    }
    textSelectRef = null;
    textSelectScope = null;
}

// ── ScriptState (Load external scripts/stylesheets) ──────────────────────────
export function loadScript(url, dotNetRef) {
    // Check if already loaded
    const existing = document.querySelector(`script[src="${url}"]`);
    if (existing) {
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnScriptLoaded');
        return;
    }
    const script = document.createElement('script');
    script.src = url;
    script.onload = () => {
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnScriptLoaded');
    };
    script.onerror = () => {
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnScriptError', url);
    };
    document.head.appendChild(script);
}

export function loadStylesheet(url) {
    const existing = document.querySelector(`link[href="${url}"]`);
    if (existing) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = url;
    document.head.appendChild(link);
}

// ── Focus trap ───────────────────────────────────────────────────────────────
const _focusTrapMap = new Map();

function _getFocusable(el) {
    return el.querySelectorAll(
        'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), '
        + 'select:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled]), [contenteditable]');
}

/** Activates a focus trap on the given element. Returns a unique trap id. */
export function activateFocusTrap(element, dotNetRef, id) {
    const trapId = id || crypto.randomUUID();
    const handler = (e) => {
        if (e.key !== 'Tab') return;
        const focusable = _getFocusable(element);
        if (focusable.length === 0) {
            e.preventDefault();
            return;
        }
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement;
        if (e.shiftKey) {
            if (active === first || !element.contains(active)) {
                e.preventDefault();
                last.focus();
            }
        } else {
            if (active === last || !element.contains(active)) {
                e.preventDefault();
                first.focus();
            }
        }
    };
    element.addEventListener('keydown', handler);
    _focusTrapMap.set(trapId, { element, handler });
    // Auto-focus first focusable
    const focusable = _getFocusable(element);
    if (focusable.length > 0) focusable[0].focus();
    return trapId;
}

/** Deactivates the focus trap for the given id. */
export function deactivateFocusTrap(id) {
    const entry = _focusTrapMap.get(id);
    if (!entry) return;
    entry.element.removeEventListener('keydown', entry.handler);
    _focusTrapMap.delete(id);
}
