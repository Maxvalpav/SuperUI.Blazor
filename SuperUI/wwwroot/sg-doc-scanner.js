// sg-doc-scanner.js — SuperUI document scanner bridge (OpenCV.js + getUserMedia)

// techstark first: it's a UMD bundle that keeps its emscripten `Module` local, so
// it can't collide with a global `Module` owned by another runtime. The
// docs.opencv.org build is a plain emscripten bundle that reads the *global*
// `Module`, which makes it the riskier of the two — hence the fallback slot.
// Versions are pinned; an unpinned jsdelivr path silently follows the `latest`
// tag across major versions (currently 5.x).
const OPENCV_SOURCES = [
    'https://cdn.jsdelivr.net/npm/@techstark/opencv-js@4.12.0-release.1/dist/opencv.js',
    'https://docs.opencv.org/4.5.5/opencv.js',
];

// opencv.js ships ~9 MB of wasm; on a cold cache over a slow link the runtime
// can take tens of seconds to instantiate. Bail out after this so the caller
// gets a rejection instead of a promise that never settles.
const CV_READY_TIMEOUT_MS = 60000;
const VIDEO_METADATA_TIMEOUT_MS = 10000;

let cvPromise = null;

function _isCvReady() {
    const cv = window.cv;
    return !!(cv && typeof cv.Mat === 'function' && typeof cv.imread === 'function');
}

function _injectScript(src) {
    return new Promise((resolve, reject) => {
        const el = document.createElement('script');
        el.src = src;
        el.async = true;
        el.onload = () => resolve();
        el.onerror = () => {
            el.remove();
            reject(new Error(`Failed to load script: ${src}`));
        };
        document.head.appendChild(el);
    });
}

// `script.onload` only means the JS wrapper was parsed — the wasm runtime is
// still starting. Builds signal readiness inconsistently (some expose `cv` as a
// Promise, older ones use `cv.onRuntimeInitialized`), and we must NOT hook
// `window.Module`: under Blazor WebAssembly that global belongs to the dotnet
// runtime. So poll for the actual API instead — build-agnostic and bounded.
function _waitForCvReady(timeoutMs) {
    return new Promise((resolve, reject) => {
        if (_isCvReady()) { resolve(window.cv); return; }

        // Some builds expose `cv` as something you await rather than the module
        // itself. Emscripten's is a bare *thenable*, not a Promise:
        //   Module.then = func => { ...func(Module); return Module }
        // Two traps here. It has no `catch`, so `cv.then(..).catch(..)` throws.
        // And it resolves with itself while keeping `then`, so handing it to
        // Promise.resolve/await recurses forever through the thenable-adoption
        // path and never settles. Hence: call `then` directly with plain
        // callbacks and never adopt it. Readiness is really established by the
        // poll below; this only helps builds whose `then` yields a *different*
        // object than the one already on `window.cv`.
        let unwrapping = false;
        const tryUnwrap = () => {
            const cv = window.cv;
            if (unwrapping || !cv || typeof cv.then !== 'function') return;
            unwrapping = true;
            try {
                // Second arg keeps a real Promise's rejection from going unhandled;
                // emscripten's thenable simply ignores it.
                cv.then(
                    mod => { if (mod && typeof mod.imread === 'function') window.cv = mod; },
                    () => { });
            } catch (_) { /* unusable `then` — polling still covers us */ }
        };

        tryUnwrap();
        const startedAt = performance.now();
        const timer = setInterval(() => {
            tryUnwrap();
            if (_isCvReady()) {
                clearInterval(timer);
                resolve(window.cv);
            } else if (performance.now() - startedAt > timeoutMs) {
                clearInterval(timer);
                reject(new Error('OpenCV.js runtime did not initialize in time'));
            }
        }, 100);
    });
}

export function loadOpenCV() {
    if (cvPromise) return cvPromise;

    cvPromise = (async () => {
        if (_isCvReady()) return window.cv;
        let lastError;
        for (const src of OPENCV_SOURCES) {
            try {
                await _injectScript(src);
                return await _waitForCvReady(CV_READY_TIMEOUT_MS);
            } catch (err) {
                lastError = err;
                console.warn(`[SgDocScanner] OpenCV source failed (${src}):`, err);
            }
        }
        throw lastError || new Error('Failed to load OpenCV.js from all sources');
    })();

    // Drop the cached rejection so a later click can retry instead of failing
    // instantly forever.
    cvPromise.catch(() => { cvPromise = null; });
    return cvPromise;
}

let _scannerState = {
    video: null,
    canvas: null,
    isActive: false,
    stream: null,
    animationFrame: null
};

// Resolves once the video element knows its dimensions. Metadata may already be
// available by the time we attach the listener, hence the readyState check.
function _waitForVideoMetadata(video) {
    if (video.readyState >= 1) return Promise.resolve();
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            video.removeEventListener('loadedmetadata', onLoaded);
            reject(new Error('Video metadata timeout'));
        }, VIDEO_METADATA_TIMEOUT_MS);
        const onLoaded = () => { clearTimeout(timer); resolve(); };
        video.addEventListener('loadedmetadata', onLoaded, { once: true });
    });
}

/// Returns { ok: true } or { ok: false, error } — the caller renders the reason
/// rather than silently falling back to the start button.
export async function startScanner(video, canvas) {
    try {
        stopScanner();
        const cv = await loadOpenCV();

        _scannerState.video = video;
        _scannerState.canvas = canvas;
        _scannerState.stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
        });

        video.srcObject = _scannerState.stream;
        await _waitForVideoMetadata(video);
        await video.play();

        _scannerState.isActive = true;
        _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
        return { ok: true, error: null };
    } catch (err) {
        console.error('[SgDocScanner] Error starting scanner:', err);
        stopScanner();
        return { ok: false, error: String(err?.message || err) };
    }
}

export function stopScanner() {
    _scannerState.isActive = false;
    if (_scannerState.stream) {
        _scannerState.stream.getTracks().forEach(track => track.stop());
        _scannerState.stream = null;
    }
    if (_scannerState.animationFrame) {
        cancelAnimationFrame(_scannerState.animationFrame);
        _scannerState.animationFrame = null;
    }
    if (_scannerState.video) {
        try { _scannerState.video.srcObject = null; } catch (_) { }
    }
}

function _processFrame(cv) {
    if (!_scannerState.isActive) return;

    const { video, canvas } = _scannerState;
    if (!video || !canvas || video.paused || video.ended || video.readyState < 2) {
        _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
        return;
    }

    try {
        const ctx = canvas.getContext('2d', { alpha: false });
        if (canvas.width !== video.videoWidth || canvas.height !== video.videoHeight) {
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
        }

        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

        let src = cv.imread(canvas);
        let dst = new cv.Mat();

        // 1. Preprocessing
        cv.cvtColor(src, dst, cv.COLOR_RGBA2GRAY);
        cv.GaussianBlur(dst, dst, new cv.Size(5, 5), 0);
        cv.Canny(dst, dst, 75, 200);

        // 2. Find contours
        let contours = new cv.MatVector();
        let hierarchy = new cv.Mat();
        cv.findContours(dst, contours, hierarchy, cv.RETR_LIST, cv.CHAIN_APPROX_SIMPLE);

        let maxArea = 0;
        let maxContourIndex = -1;
        let approx = new cv.Mat();

        for (let i = 0; i < contours.size(); ++i) {
            let cnt = contours.get(i);
            let area = cv.contourArea(cnt);
            if (area > (canvas.width * canvas.height * 0.05)) { // Минимум 5% площади экрана
                let peri = cv.arcLength(cnt, true);
                cv.approxPolyDP(cnt, approx, 0.02 * peri, true);
                if (approx.rows === 4 && area > maxArea) {
                    maxArea = area;
                    maxContourIndex = i;
                }
            }
        }

        // 3. Draw detected contour
        if (maxContourIndex !== -1) {
            let cnt = contours.get(maxContourIndex);
            let color = new cv.Scalar(0, 255, 0, 255);
            let matVec = new cv.MatVector();
            matVec.push_back(cnt);
            cv.drawContours(src, matVec, -1, color, 3);
            cv.imshow(canvas, src);
            matVec.delete();
        } else {
            // Если контур не найден, просто показываем видео
            cv.imshow(canvas, src);
        }

        src.delete();
        dst.delete();
        contours.delete();
        hierarchy.delete();
        approx.delete();
    } catch (e) {
        console.error('[SgDocScanner] Processing error:', e);
    }

    _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
}

export async function captureAndProcess() {
    const cv = await loadOpenCV();
    const { canvas } = _scannerState;
    if (!canvas) return null;

    // Временно останавливаем трекинг для финальной обработки
    _scannerState.isActive = false;

    let src = cv.imread(canvas);
    let dst = new cv.Mat();

    // Финальная обработка: Binarization (Otsu) для чистого документа
    cv.cvtColor(src, dst, cv.COLOR_RGBA2GRAY);
    cv.threshold(dst, dst, 0, 255, cv.THRESH_BINARY | cv.THRESH_OTSU);

    cv.imshow(canvas, dst);
    const dataUrl = canvas.toDataURL('image/png');

    src.delete();
    dst.delete();

    // Возвращаем поток в работу (если не закрываем)
    if (_scannerState.stream) {
        _scannerState.isActive = true;
        _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
    }

    return dataUrl;
}
