let cvPromise = null;

export function loadOpenCV() {
    if (cvPromise) return cvPromise;
    
    cvPromise = new Promise((resolve, reject) => {
        if (window.cv) {
            resolve(window.cv);
            return;
        }
        
        console.log('[SgDocScanner] Loading OpenCV.js...');
        const script = document.createElement('script');
        script.src = 'https://docs.opencv.org/4.5.4/opencv.js';
        script.async = true;
        script.onload = () => {
            if (window.cv.RuntimeError) {
                window.cv.onRuntimeInitialized = () => resolve(window.cv);
            } else {
                resolve(window.cv);
            }
        };
        script.onerror = () => reject(new Error('Failed to load OpenCV.js'));
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
    const cv = await loadOpenCV();
    _scannerState.video = video;
    _scannerState.canvas = canvas;
    
    try {
        _scannerState.stream = await navigator.mediaDevices.getUserMedia({ 
            video: { facingMode: 'environment' } 
        });
        video.srcObject = _scannerState.stream;
        video.play();
        _scannerState.isActive = true;
        
        requestAnimationFrame(() => _processFrame(cv));
        return true;
    } catch (err) {
        console.error('[SgDocScanner] Error accessing camera:', err);
        return false;
    }
}

export function stopScanner() {
    _scannerState.isActive = false;
    if (_scannerState.stream) {
        _scannerState.stream.getTracks().forEach(track => track.stop());
    }
    if (_scannerState.animationFrame) {
        cancelAnimationFrame(_scannerState.animationFrame);
    }
}

function _processFrame(cv) {
    if (!_scannerState.isActive) return;
    
    const { video, canvas } = _scannerState;
    if (!video || !canvas || video.paused || video.ended) {
        _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
        return;
    }

    const ctx = canvas.getContext('2d');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
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
        if (area > 5000) {
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
    }

    src.delete();
    dst.delete();
    contours.delete();
    hierarchy.delete();
    approx.delete();

    _scannerState.animationFrame = requestAnimationFrame(() => _processFrame(cv));
}

export async function captureAndProcess() {
    const cv = await loadOpenCV();
    const { video, canvas } = _scannerState;
    
    // Final high-quality processing
    let src = cv.imread(canvas);
    let dst = new cv.Mat();
    cv.cvtColor(src, dst, cv.COLOR_RGBA2GRAY);
    cv.threshold(dst, dst, 0, 255, cv.THRESH_BINARY | cv.THRESH_OTSU);
    
    cv.imshow(canvas, dst);
    const dataUrl = canvas.toDataURL('image/png');
    
    src.delete();
    dst.delete();
    
    return dataUrl;
}
