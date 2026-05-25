let terminals = new Map();

export async function initTerminal(container, options, dotNetHelper) {
    if (!window.Terminal) {
        await loadXterm();
    }

    const termOptions = {
        cursorBlink: true,
        fontSize: options.fontSize || 14,
        fontFamily: options.fontFamily || 'Consolas, "Courier New", monospace',
        convertEol: true
    };

    // Только если тема передана и не пуста, применяем её
    if (options.theme && Object.keys(options.theme).length > 0) {
        // Очищаем null/undefined значения из темы
        const cleanTheme = {};
        for (const [key, value] of Object.entries(options.theme)) {
            if (value) cleanTheme[key.toLowerCase()] = value;
        }
        if (Object.keys(cleanTheme).length > 0) {
            termOptions.theme = cleanTheme;
        }
    }

    if (!termOptions.theme) {
        termOptions.theme = {
            background: '#1e1e1e',
            foreground: '#cccccc'
        };
    }

    const term = new Terminal(termOptions);

    const fitAddon = new FitAddon.FitAddon();
    term.loadAddon(fitAddon);
    
    term.open(container);
    fitAddon.fit();

    const terminalId = Math.random().toString(36).substr(2, 9);
    
    term.onData(data => {
        dotNetHelper.invokeMethodAsync('OnDataReceived', data);
    });

    const resizeObserver = new ResizeObserver(() => {
        fitAddon.fit();
    });
    resizeObserver.observe(container);

    terminals.set(terminalId, {
        term,
        fitAddon,
        resizeObserver
    });

    return terminalId;
}

export function write(terminalId, data) {
    const t = terminals.get(terminalId);
    if (t) t.term.write(data);
}

export function writeln(terminalId, data) {
    const t = terminals.get(terminalId);
    if (t) t.term.writeln(data);
}

export function clear(terminalId) {
    const t = terminals.get(terminalId);
    if (t) t.term.clear();
}

export function dispose(terminalId) {
    const t = terminals.get(terminalId);
    if (t) {
        t.resizeObserver.disconnect();
        t.term.dispose();
        terminals.delete(terminalId);
    }
}

async function loadXterm() {
    return new Promise((resolve, reject) => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = 'https://cdn.jsdelivr.net/npm/xterm@5.3.0/css/xterm.css';
        document.head.appendChild(link);

        const scripts = [
            'https://cdn.jsdelivr.net/npm/xterm@5.3.0/lib/xterm.js',
            'https://cdn.jsdelivr.net/npm/xterm-addon-fit@0.8.0/lib/xterm-addon-fit.js'
        ];

        let loaded = 0;
        scripts.forEach(src => {
            const script = document.createElement('script');
            script.src = src;
            script.onload = () => {
                loaded++;
                if (loaded === scripts.length) resolve();
            };
            script.onerror = reject;
            document.head.appendChild(script);
        });
    });
}
