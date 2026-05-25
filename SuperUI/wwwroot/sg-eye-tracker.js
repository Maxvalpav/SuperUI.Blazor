let webgazerPromise = null;
let _isInitialized = false;

export function loadWebGazer() {
    if (webgazerPromise) return webgazerPromise;
    
    webgazerPromise = new Promise((resolve, reject) => {
        if (window.webgazer) {
            resolve(window.webgazer);
            return;
        }
        
        console.log('[SgEyeTracker] Loading WebGazer.js...');
        const script = document.createElement('script');
        // Используем CDN версию для большей стабильности
        script.src = 'https://cdn.jsdelivr.net/npm/webgazer@2.1.0/dist/webgazer.js';
        script.async = true;
        script.onload = () => {
            if (window.webgazer) {
                console.log('[SgEyeTracker] WebGazer.js loaded successfully');
                resolve(window.webgazer);
            } else {
                reject(new Error('WebGazer object not found after script load'));
            }
        };
        script.onerror = () => {
            webgazerPromise = null;
            reject(new Error('Failed to load WebGazer.js from CDN'));
        };
        document.head.appendChild(script);
    });
    
    return webgazerPromise;
}

let _dot = null;
let _dotInner = null;

export async function initEyeTracker(dotElement, dotInnerElement) {
    try {
        const webgazer = await loadWebGazer();
        _dot = dotElement;
        _dotInner = dotInnerElement;

        if (_isInitialized) {
            console.log('[SgEyeTracker] WebGazer already initialized, resuming...');
            webgazer.resume();
            return true;
        }

        console.log('[SgEyeTracker] Initializing WebGazer...');
        
        // Настройка слушателя
        webgazer.setGazeListener((data, elapsedTime) => {
            if (data == null || !_dot) return;

            const x = data.x;
            const y = data.y;

            // Плавное перемещение точки через CSS
            _dot.style.transform = `translate3d(${x}px, ${y}px, 0)`;
        });

        // Запуск
        await webgazer.begin();
        
        // Скрываем стандартные элементы UI WebGazer
        webgazer.showVideo(false)
                .showPredictionPoints(false);
        
        // Отключаем логирование в консоль для производительности
        webgazer.setStatic(true); 

        _isInitialized = true;
        console.log('[SgEyeTracker] WebGazer initialized and started');
        return true;
    } catch (err) {
        console.error('[SgEyeTracker] Initialization error:', err);
        throw err;
    }
}

export function stopEyeTracker() {
    if (window.webgazer) {
        console.log('[SgEyeTracker] Stopping WebGazer...');
        window.webgazer.pause();
        // Мы используем pause вместо end(), чтобы избежать полной выгрузки моделей
        // если пользователь захочет включить трекер снова.
        if (_dot) {
            _dot.style.transform = `translate3d(-100px, -100px, 0)`;
        }
    }
}

export function calibrate() {
    if (window.webgazer) {
        window.webgazer.clearData(); // Сброс старой калибровки при необходимости
        alert('Инструкция по калибровке:\n1. Следите взглядом за курсором.\n2. Кликните мышью в 5-9 разных точках экрана.\n3. Старайтесь не двигать головой.');
    }
}

export function dispose() {
    if (window.webgazer) {
        window.webgazer.end();
        _isInitialized = false;
        webgazerPromise = null;
    }
}
