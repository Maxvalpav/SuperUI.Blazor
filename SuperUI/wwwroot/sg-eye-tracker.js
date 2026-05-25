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
let _dwellTimer = null;
let _currentElement = null;
let _dwellStartTime = 0;
const DWELL_DURATION = 1000; // ms
const GHOST_CURSOR_RADIUS = 30; // pixels to consider "staying"

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
            if (_dot && _dot.style) {
                _dot.style.transform = `translate3d(${x}px, ${y}px, 0)`;
            }

            // Логика задержки взгляда (Gaze-dwell)
            handleDwell(x, y);
        });

        // Запуск
        await webgazer.begin();
        
        // Скрываем стандартные элементы UI WebGazer
        if (typeof webgazer.showVideo === 'function') webgazer.showVideo(false);
        if (typeof webgazer.showPredictionPoints === 'function') webgazer.showPredictionPoints(false);
        
        // Отключаем логирование в консоль для производительности
        if (typeof webgazer.setStatic === 'function') {
            webgazer.setStatic(true); 
        }

        _isInitialized = true;
        console.log('[SgEyeTracker] WebGazer initialized and started');
        return true;
    } catch (err) {
        console.error('[SgEyeTracker] Initialization error:', err);
        // Не выбрасываем ошибку, чтобы не ломать Blazor, но возвращаем false
        return false;
    }
}

function handleDwell(x, y) {
    if (!_dot) return;
    
    // Получаем элемент под курсором взгляда
    // Скрываем курсор на мгновение, чтобы не поймать его самого
    _dot.style.pointerEvents = 'none';
    const element = document.elementFromPoint(x, y);
    _dot.style.pointerEvents = 'auto'; // Возвращаем обратно

    // Нас интересуют интерактивные элементы или те, что помечены как gaze-target
    const interactive = element && (
        element.classList.contains('gaze-target') || 
        element.tagName === 'BUTTON' || 
        element.tagName === 'A' ||
        element.closest('.gaze-target')
    );

    if (interactive && element === _currentElement) {
        const elapsed = Date.now() - _dwellStartTime;
        const percent = Math.min(100, (elapsed / DWELL_DURATION) * 100);
        
        // Визуальное обновление прогресса
        _dot.style.setProperty('--dwell-progress', `${percent}%`);
        
        if (elapsed >= DWELL_DURATION) {
            // Клик!
            triggerGazeClick(element);
            resetDwell();
        }
    } else if (interactive) {
        // Новый элемент
        _currentElement = element;
        _dwellStartTime = Date.now();
        _dot.classList.add('is-dwelling');
    } else {
        // Взгляд ушел с интерактивного элемента
        resetDwell();
    }
}

function triggerGazeClick(element) {
    if (!element || !_dot) return;
    
    const rect = _dot.getBoundingClientRect();
    const clickEvent = new MouseEvent('click', {
        view: window,
        bubbles: true,
        cancelable: true,
        clientX: rect.left + rect.width / 2,
        clientY: rect.top + rect.height / 2
    });
    
    element.dispatchEvent(clickEvent);
    
    // Эффект всплеска
    _dot.classList.add('gaze-clicked');
    setTimeout(() => {
        if (_dot) _dot.classList.remove('gaze-clicked');
    }, 300);
}

function resetDwell() {
    _currentElement = null;
    _dwellStartTime = 0;
    if (_dot) {
        _dot.classList.remove('is-dwelling');
        _dot.style.setProperty('--dwell-progress', '0%');
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
