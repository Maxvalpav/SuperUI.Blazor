// SgMonaco — Monaco Editor wrapper for SuperUI Blazor
const _instances = new Map();
let _monacoReady = false;
let _monacoLoading = false;
const _monacoQueue = [];

function _loadMonaco(sources) {
    return new Promise((resolve, reject) => {
        if (_monacoReady && window.monaco) { resolve(window.monaco); return; }
        _monacoQueue.push({ resolve, reject });
        if (_monacoLoading) return;
        _monacoLoading = true;

        const loaderUrl = sources?.loaderScript || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js';
        const vsPath    = sources?.vsPath       || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs';

        const s = document.createElement('script');
        s.src = loaderUrl;
        s.onload = () => {
            window.require.config({ paths: { vs: vsPath } });
            window.require(['vs/editor/editor.main'], () => {
                _monacoReady = true;
                const q = _monacoQueue.splice(0);
                q.forEach(p => p.resolve(window.monaco));
            });
        };
        s.onerror = () => {
            const err = new Error('Failed to load Monaco Editor');
            _monacoQueue.forEach(p => p.reject(err));
            _monacoQueue.length = 0;
            _monacoLoading = false;
        };
        document.head.appendChild(s);
    });
}

export async function initEditor(dotnetRef, containerRef, instanceId, opts, initialValue, sources) {
    await disposeEditor(instanceId);
    const monaco = await _loadMonaco(sources);

    const editor = monaco.editor.create(containerRef, {
        value:       initialValue ?? '',
        language:    opts.language ?? 'json',
        theme:       opts.theme    ?? 'vs',
        fontSize:    opts.fontSize ?? 13,
        readOnly:    opts.readOnly ?? false,
        minimap:     { enabled: opts.minimap ?? false },
        lineNumbers: opts.lineNumbers !== false ? 'on' : 'off',
        wordWrap:    opts.wordWrap ? 'on' : 'off',
        automaticLayout: true,
        scrollBeyondLastLine: false,
        renderLineHighlight: 'line',
        padding: { top: 8, bottom: 8 },
        fontFamily: "'Fira Code', 'Cascadia Code', Consolas, monospace",
        fontLigatures: true,
        tabSize: 2,
        formatOnPaste: true,
        formatOnType: false,
    });

    // Auto-format JSON on init
    if ((opts.language === 'json' || !opts.language) && opts.autoFormat !== false) {
        setTimeout(() => {
            try { editor.getAction('editor.action.formatDocument')?.run(); } catch {}
        }, 300);
    }

    // Change handler — debounced, skip programmatic changes
    let debounce = null;
    editor.onDidChangeModelContent(() => {
        if (_instances.get(instanceId)?._settingValue) return;
        clearTimeout(debounce);
        debounce = setTimeout(() => {
            try { dotnetRef.invokeMethodAsync('OnValueChangedAsync', editor.getValue()); } catch {}
        }, 300);
    });

    // Resize observer
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { editor.layout(); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { editor, monaco, dotnetRef, ro });
}

export function getValue(instanceId) {
    return _instances.get(instanceId)?.editor.getValue() ?? '';
}

export function setValue(instanceId, value) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const model = inst.editor.getModel();
    if (!model) return;

    // Temporarily disable readOnly to allow setValue, then restore
    const wasReadOnly = inst.editor.getOption(inst.monaco.editor.EditorOption.readOnly);
    if (wasReadOnly) inst.editor.updateOptions({ readOnly: false });

    // Use pushEditOperations to avoid triggering change events
    inst._settingValue = true;
    model.pushEditOperations([], [{
        range: model.getFullModelRange(),
        text: value ?? '',
    }], () => null);
    inst._settingValue = false;

    if (wasReadOnly) inst.editor.updateOptions({ readOnly: true });

    // Auto-format after setValue if JSON
    const lang = model.getLanguageId();
    if (lang === 'json') {
        setTimeout(() => {
            try { inst.editor.getAction('editor.action.formatDocument')?.run(); } catch {}
        }, 100);
    }
}

export function setLanguage(instanceId, language) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.monaco.editor.setModelLanguage(inst.editor.getModel(), language);
}

export function setTheme(instanceId, theme) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.monaco.editor.setTheme(theme);
}

export function format(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.editor.getAction('editor.action.formatDocument')?.run(); } catch {}
}

export function setReadOnly(instanceId, readOnly) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.editor.updateOptions({ readOnly });
}

export function layout(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.editor.layout(); } catch {}
}

export function disposeEditor(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.editor.dispose(); } catch {}
    _instances.delete(instanceId);
}
