let cvPromise = null;

export function loadOpenCV() {
    if (cvPromise) return cvPromise;
    
    cvPromise = new Promise((resolve, reject) => {
        if (window.cv && window.cv.onRuntimeInitialized === undefined) {
            resolve(window.cv);
            return;
        }
        
        console.log('[SgDocScanner] Loading OpenCV.js from CDN...');
        const script = document.createElement('script');
        // Используем более стабильный CDN
        script.src = 'https://cdn.jsdelivr.net/npm/@techstark/opencv-js@4.6.0.1/dist/opencv.js';
        script.async = true;
        
        window.Module = {
            onRuntimeInitialized: () => {
                console.log('[SgDocScanner] OpenCV.js Runtime Initialized');
                resolve(window.cv);
            }
        };

        script.onload = () => {
            console.log('[SgDocScanner] OpenCV.js script loaded');
            // В некоторых сборках cv инициализируется сразу
            if (window.cv && window.cv.Mat) {
                resolve(window.cv);
            }
        };
        
        script.onerror = () => {
            cvPromise = null;
            reject(new Error('Failed to load OpenCV.js'));
        };
        document.head.appendChild(script);
    });
    
    return cvPromise;
}

let _scannerState = {
    video: null,
    canvas: null,
    isActive: false,
    stream: null,
    animationFrame: null
};

export async function startScanner(video, canvas) {
    try {
        console.log('[SgDocScanner] Starting scanner...');
        const cv = await loadOpenCV();
        _scannerState.video = video;
        _scannerState.canvas = canvas;
        
        _scannerState.stream = await navigator.mediaDevices.getUserMedia({ 
            video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } } 
        });
        
        video.srcObject = _scannerState.stream;
        
        return new Promise((resolve) => {
            video.onloadedmetadata = () => {
                video.play();
                _scannerState.isActive = true;
                console.log('[SgDocScanner] Camera stream active');
                requestAnimationFrame(() => _processFrame(cv));
                resolve(true);
            };
        });
    } catch (err) {
        console.error('[SgDocScanner] Error starting scanner:', err);
        return false;
    }
}

export function stopScanner() {
    console.log('[SgDocScanner] Stopping scanner...');
    _scannerState.isActive = false;
    if (_scannerState.stream) {
        _scannerState.stream.getTracks().forEach(track => track.stop());
        _scannerState.stream = null;
    }
    if (_scannerState.animationFrame) {
        cancelAnimationFrame(_scannerState.animationFrame);
        _scannerState.animationFrame = null;
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
    const { video, canvas } = _scannerState;
    
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
    _scannerState.isActive = true;
    requestAnimationFrame(() => _processFrame(cv));
    
    return dataUrl;
}
