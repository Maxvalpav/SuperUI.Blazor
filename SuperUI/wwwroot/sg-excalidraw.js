let excalidrawReady = null;

async function loadDependencies() {
    if (excalidrawReady) return excalidrawReady;

    excalidrawReady = (async () => {
        const scripts = [
            'https://unpkg.com/react@18.2.0/umd/react.production.min.js',
            'https://unpkg.com/react-dom@18.2.0/umd/react-dom.production.min.js',
            'https://unpkg.com/@excalidraw/excalidraw@0.17.3/dist/excalidraw.production.min.js'
        ];

        for (const src of scripts) {
            // Проверяем, не загружен ли уже скрипт (например, другой библиотекой)
            if (src.includes('react.production') && window.React) continue;
            if (src.includes('react-dom.production') && window.ReactDOM) continue;
            if (src.includes('excalidraw.production') && window.ExcalidrawLib) continue;

            await new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = src;
                script.async = false;
                script.crossOrigin = "anonymous";
                script.onload = resolve;
                script.onerror = (e) => {
                    console.error(`[SgExcalidraw] Failed to load script: ${src}`, e);
                    reject(e);
                };
                document.head.appendChild(script);
            });
        }
    })();

    return excalidrawReady;
}

let instances = new Map();

export async function initExcalidraw(container, dotNetHelper, options) {
    console.log('[SgExcalidraw] Initializing for container:', container.id);
    try {
        await loadDependencies();

        const React = window.React;
        const ReactDOM = window.ReactDOM;
        const ExcalidrawLib = window.ExcalidrawLib;

        if (!React || !ReactDOM || !ExcalidrawLib) {
            console.error('[SgExcalidraw] Dependencies not found in window object:', { React: !!React, ReactDOM: !!ReactDOM, ExcalidrawLib: !!ExcalidrawLib });
            return;
        }

        const Excalidraw = ExcalidrawLib.Excalidraw;
        if (!Excalidraw) {
            console.error('[SgExcalidraw] Excalidraw component not found in ExcalidrawLib');
            return;
        }

        const App = () => {
            const [excalidrawAPI, setExcalidrawAPI] = React.useState(null);

            React.useEffect(() => {
                if (excalidrawAPI) {
                    console.log('[SgExcalidraw] API ready for:', container.id);
                    instances.set(container.id, excalidrawAPI);
                    dotNetHelper.invokeMethodAsync('OnReadyCallback');
                }
            }, [excalidrawAPI]);

            return React.createElement(
                'div',
                { style: { height: '100%', width: '100%' } },
                React.createElement(Excalidraw, {
                    ref: (api) => setExcalidrawAPI(api),
                    initialData: options.initialData,
                    theme: options.theme || 'light'
                })
            );
        };

        const root = ReactDOM.createRoot(container);
        root.render(React.createElement(App));
        
        instances.set(`${container.id}_root`, root);
    } catch (e) {
        console.error('[SgExcalidraw] Initialization failed:', e);
    }
}

export async function exportToBlob(containerId, options) {
    const api = instances.get(containerId);
    if (!api) return null;

    const ExcalidrawLib = window.ExcalidrawLib;
    const elements = api.getSceneElements();
    
    const blob = await ExcalidrawLib.exportToBlob({
        elements,
        mimeType: options.mimeType || 'image/png',
        appState: {
            ...api.getAppState(),
            exportBackground: true,
        },
    });

    const reader = new FileReader();
    return new Promise((resolve) => {
        reader.onloadend = () => resolve(reader.result.split(',')[1]);
        reader.readAsDataURL(blob);
    });
}

export function updateScene(containerId, data) {
    const api = instances.get(containerId);
    if (api) {
        api.updateScene(data);
    }
}

export function dispose(containerId) {
    const root = instances.get(`${containerId}_root`);
    if (root) {
        try {
            root.unmount();
        } catch (e) {
            console.warn('[SgExcalidraw] Error during unmount:', e);
        }
        instances.delete(containerId);
        instances.delete(`${containerId}_root`);
    }
}
