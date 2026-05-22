// SgMonaco — Monaco Editor wrapper for SuperUI Blazor.
//
// Robustness notes:
//   • Single loader promise — re-entrant calls during prerender / hot-reload
//     share the same resolution, never re-inject vs/loader.js or re-execute
//     vs/editor/editor.main (the second exec is what throws inside `new o(...)`
//     when Blazor remounts the component).
//   • Container ref is validated before `monaco.editor.create` — a detached
//     node would throw deep inside Monaco's HTMLBodyElement init.
//   • Theme syncs to SuperUI's [data-theme] attribute on <html> automatically.
//   • setValue uses executeEdits with a single edit op so undo/redo keeps
//     working across external value pushes.

const _instances = new Map();
let _loaderPromise = null;
let _themeObserver = null;

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadMonaco(sources) {
    if (_loaderPromise) return _loaderPromise;

    _loaderPromise = new Promise((resolve, reject) => {
        // Already loaded (e.g. by another consumer) — reuse.
        if (window.monaco && window.monaco.editor) {
            resolve(window.monaco);
            return;
        }

        const loaderUrl = sources?.loaderScript || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js';
        const vsPath    = sources?.vsPath       || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs';

        const finalize = () => {
            try {
                window.require.config({ paths: { vs: vsPath } });
                window.require(['vs/editor/editor.main'], () => {
                    if (window.monaco && window.monaco.editor) {
                        _registerCustomThemes(window.monaco);
                        resolve(window.monaco);
                    } else {
                        reject(new Error('Monaco loaded but window.monaco is unavailable.'));
                    }
                }, (err) => reject(err instanceof Error ? err : new Error(String(err))));
            } catch (err) {
                reject(err);
            }
        };

        // Reuse an existing loader.js tag if one is already present.
        const existing = document.querySelector(`script[data-sg-monaco-loader="1"]`);
        if (existing) {
            if (window.require && window.require.config) finalize();
            else existing.addEventListener('load', finalize, { once: true });
            return;
        }

        const s = document.createElement('script');
        s.src = loaderUrl;
        s.async = true;
        s.dataset.sgMonacoLoader = '1';
        s.onload = finalize;
        s.onerror = () => {
            _loaderPromise = null; // allow retry next time
            reject(new Error(`Failed to load Monaco loader from ${loaderUrl}`));
        };
        document.head.appendChild(s);
    });

    // If loader fails, don't poison future retries.
    _loaderPromise.catch(() => { _loaderPromise = null; });
    return _loaderPromise;
}

function _registerCustomThemes(monaco) {
    if (monaco._sgThemesReady) return;
    try {
        // Subtle SuperUI-tinted variants that pick up our slate/blue palette.
        monaco.editor.defineTheme('sg-light', {
            base: 'vs',
            inherit: true,
            rules: [],
            colors: {
                'editor.background':              '#ffffff',
                'editor.foreground':              '#0f172a',
                'editorLineNumber.foreground':    '#94a3b8',
                'editorLineNumber.activeForeground': '#2563eb',
                'editor.lineHighlightBackground': '#f8fafc',
                'editorCursor.foreground':        '#2563eb',
                'editor.selectionBackground':     '#dbeafe',
                'editor.inactiveSelectionBackground': '#e2e8f0',
                'editorIndentGuide.background':   '#e2e8f0',
                'editorIndentGuide.activeBackground': '#cbd5e1',
                'editorBracketMatch.background':  '#dbeafe',
                'editorBracketMatch.border':      '#2563eb',
                'editorWidget.background':        '#ffffff',
                'editorWidget.border':            '#e2e8f0',
                'scrollbarSlider.background':     'rgba(15, 23, 42, 0.16)',
                'scrollbarSlider.hoverBackground':'rgba(15, 23, 42, 0.28)',
                'scrollbarSlider.activeBackground':'rgba(15, 23, 42, 0.36)',
            }
        });
        monaco.editor.defineTheme('sg-dark', {
            base: 'vs-dark',
            inherit: true,
            rules: [],
            colors: {
                'editor.background':              '#0f172a',
                'editor.foreground':              '#f1f5f9',
                'editorLineNumber.foreground':    '#475569',
                'editorLineNumber.activeForeground': '#60a5fa',
                'editor.lineHighlightBackground': '#1e293b',
                'editorCursor.foreground':        '#60a5fa',
                'editor.selectionBackground':     'rgba(96, 165, 250, 0.25)',
                'editorIndentGuide.background':   '#1e293b',
                'editorIndentGuide.activeBackground': '#334155',
                'editorBracketMatch.background':  'rgba(96, 165, 250, 0.18)',
                'editorBracketMatch.border':      '#60a5fa',
                'editorWidget.background':        '#0f172a',
                'editorWidget.border':            '#1e293b',
                'scrollbarSlider.background':     'rgba(255, 255, 255, 0.10)',
                'scrollbarSlider.hoverBackground':'rgba(255, 255, 255, 0.18)',
                'scrollbarSlider.activeBackground':'rgba(255, 255, 255, 0.26)',
            }
        });
        monaco._sgThemesReady = true;
    } catch { /* defineTheme is best-effort */ }
}

function _detectSgTheme() {
    try {
        const html = document.documentElement;
        const mode = html.getAttribute('data-theme');
        if (mode === 'dark') return 'sg-dark';
        if (mode === 'light') return 'sg-light';
        // auto / unset → follow OS
        return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'sg-dark' : 'sg-light';
    } catch {
        return 'sg-light';
    }
}

function _wireThemeObserver(monaco) {
    if (_themeObserver) return;
    _themeObserver = new MutationObserver(() => {
        const t = _detectSgTheme();
        try { monaco.editor.setTheme(t); } catch { /* swallow */ }
    });
    _themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
}

// ── Lifecycle ─────────────────────────────────────────────────────────────────

export async function initEditor(dotnetRef, containerRef, instanceId, opts, initialValue, sources) {
    // Idempotency: dispose any prior instance with the same id.
    await disposeEditor(instanceId);

    // Validate container — Monaco crashes hard if the element is detached.
    if (!containerRef || !(containerRef instanceof HTMLElement) || !containerRef.isConnected) {
        throw new Error('Container element is not connected to the DOM.');
    }

    const monaco = await _loadMonaco(sources);

    // Pick a theme. If the caller specified one (e.g. "vs-dark") respect it;
    // otherwise sync to SuperUI's data-theme attribute and watch for changes.
    let theme = opts?.theme;
    let trackSgTheme = false;
    if (!theme || theme === 'sg-auto') {
        theme = _detectSgTheme();
        trackSgTheme = true;
    }

    const editor = monaco.editor.create(containerRef, {
        value:                  initialValue ?? '',
        language:               opts?.language ?? 'json',
        theme,
        fontSize:               opts?.fontSize ?? 13,
        readOnly:               opts?.readOnly ?? false,
        minimap:                { enabled: opts?.minimap ?? false },
        lineNumbers:            opts?.lineNumbers !== false ? 'on' : 'off',
        wordWrap:               opts?.wordWrap ? 'on' : 'off',
        automaticLayout:        true,
        scrollBeyondLastLine:   false,
        smoothScrolling:        true,
        cursorBlinking:         'smooth',
        cursorSmoothCaretAnimation: 'on',
        renderLineHighlight:    'line',
        renderWhitespace:       'selection',
        roundedSelection:       false,
        padding:                { top: 10, bottom: 10 },
        fontFamily:             opts?.fontFamily
                                || "'JetBrains Mono', 'Fira Code', 'Cascadia Code', ui-monospace, SFMono-Regular, Consolas, monospace",
        fontLigatures:          opts?.fontLigatures !== false,
        tabSize:                opts?.tabSize ?? 2,
        insertSpaces:           true,
        formatOnPaste:          true,
        formatOnType:           false,
        bracketPairColorization:{ enabled: true },
        guides:                 { bracketPairs: 'active', indentation: true },
        scrollbar: {
            useShadows: false,
            verticalScrollbarSize: 10,
            horizontalScrollbarSize: 10,
        },
        overviewRulerLanes: 0,
        overviewRulerBorder: false,
        hideCursorInOverviewRuler: true,
    });

    if (trackSgTheme) _wireThemeObserver(monaco);

    // Auto-format JSON shortly after init, once the model has parsed.
    if ((opts?.language === 'json' || !opts?.language) && opts?.autoFormat !== false) {
        setTimeout(() => {
            try { editor.getAction('editor.action.formatDocument')?.run(); } catch {}
        }, 250);
    }

    // Change handler — debounced, skip programmatic changes.
    let debounce = null;
    const changeSub = editor.onDidChangeModelContent(() => {
        const inst = _instances.get(instanceId);
        if (!inst || inst._settingValue) return;
        clearTimeout(debounce);
        debounce = setTimeout(() => {
            try { dotnetRef.invokeMethodAsync('OnValueChangedAsync', editor.getValue()); }
            catch { /* component disposed mid-flight */ }
        }, 250);
    });

    // Resize observer — automaticLayout already handles most cases but a
    // container that resizes via a sibling can still skip; this is a belt.
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { editor.layout(); } catch {} });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, {
        editor, monaco, dotnetRef, ro,
        changeSub,
        _settingValue: false,
        _debounce: debounce,
    });
}

export function getValue(instanceId) {
    return _instances.get(instanceId)?.editor.getValue() ?? '';
}

export function setValue(instanceId, value) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const model = inst.editor.getModel();
    if (!model) return;

    const next = value ?? '';
    if (model.getValue() === next) return; // no-op — keeps undo stack clean

    // Use executeEdits to keep undo/redo working, and silence change event.
    const wasReadOnly = inst.editor.getOption(inst.monaco.editor.EditorOption.readOnly);
    if (wasReadOnly) inst.editor.updateOptions({ readOnly: false });

    inst._settingValue = true;
    try {
        inst.editor.executeEdits('sg-monaco-external', [{
            range: model.getFullModelRange(),
            text: next,
            forceMoveMarkers: true,
        }]);
    } finally {
        inst._settingValue = false;
    }

    if (wasReadOnly) inst.editor.updateOptions({ readOnly: true });

    // Auto-format JSON after external set.
    if (model.getLanguageId() === 'json') {
        setTimeout(() => {
            try { inst.editor.getAction('editor.action.formatDocument')?.run(); } catch {}
        }, 100);
    }
}

export function setLanguage(instanceId, language) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const model = inst.editor.getModel();
    if (model) inst.monaco.editor.setModelLanguage(model, language);
}

export function setTheme(instanceId, theme) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const t = (!theme || theme === 'sg-auto') ? _detectSgTheme() : theme;
    inst.monaco.editor.setTheme(t);
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

export function setFontSize(instanceId, size) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.editor.updateOptions({ fontSize: size });
}

export function layout(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.editor.layout(); } catch {}
}

export function focus(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.editor.focus(); } catch {}
}

export function disposeEditor(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { inst.changeSub?.dispose(); } catch {}
    try { inst.editor.dispose(); } catch {}
    _instances.delete(instanceId);
}
