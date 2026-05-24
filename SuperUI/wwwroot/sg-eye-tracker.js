let webgazerPromise = null;

export function loadWebGazer() {
    if (webgazerPromise) return webgazerPromise;
    
    webgazerPromise = new Promise((resolve, reject) => {
        if (window.webgazer) {
            resolve(window.webgazer);
            return;
        }
        
        console.log('[SgEyeTracker] Loading WebGazer.js...');
        const script = document.createElement('script');
        script.src = 'https://webgazer.cs.brown.edu/webgazer.js';
        script.async = true;
        script.onload = () => {
            resolve(window.webgazer);
        };
        script.onerror = () => reject(new Error('Failed to load WebGazer.js'));
        document.head.appendChild(script);
    });
    
    return webgazerPromise;
}

let _dot = null;
let _dotInner = null;

export async function initEyeTracker(dotElement, dotInnerElement) {
    const webgazer = await loadWebGazer();
    _dot = dotElement;
    _dotInner = dotInnerElement;

    webgazer.setGazeListener((data, elapsedTime) => {
        if (data == null) return;

        const x = data.x;
        const y = data.y;

        if (_dot) {
            _dot.style.transform = `translate(${x}px, ${y}px)`;
        }
    }).begin();

    // Hide webgazer video and canvas
    webgazer.showVideo(false)
            .showPredictionPoints(false);
            
    return true;
}

export function stopEyeTracker() {
    if (window.webgazer) {
        window.webgazer.end();
    }
}

export function calibrate() {
    alert('Пожалуйста, кликните в 5-9 разных точках экрана, удерживая взгляд на курсоре, для калибровки трекера.');
}
