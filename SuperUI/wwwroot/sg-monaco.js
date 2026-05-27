// SgMonaco — Monaco Editor wrapper for SuperUI Blazor.
//
// • Single loader promise — re-entrant calls share same resolution.
// • Container ref validated before create — detached node would crash.
// • Theme syncs to SuperUI [data-theme] on <html> automatically.
// • Cursor position, focus, and blur events reported to .NET.

const _instances = new Map();
let _loaderPromise = null;
let _themeObserver = null;
const _MONACO_VERSION = '0.55.1';
const _CDN_CHAINS = [
    { loader: `https://cdn.jsdelivr.net/npm/monaco-editor@${_MONACO_VERSION}/min/vs/loader.js`, vs: `https://cdn.jsdelivr.net/npm/monaco-editor@${_MONACO_VERSION}/min/vs` },
    { loader: `https://unpkg.com/monaco-editor@${_MONACO_VERSION}/min/vs/loader.js`,        vs: `https://unpkg.com/monaco-editor@${_MONACO_VERSION}/min/vs` },
];

function _loadMonaco(sources) {
    if (_loaderPromise) return _loaderPromise;
    const chains = [];
    if (sources?.loaderScript && sources?.vsPath)
        chains.push({ loader: sources.loaderScript, vs: sources.vsPath });
    chains.push(..._CDN_CHAINS);
    _loaderPromise = _tryLoadChain(chains, 0);
    _loaderPromise.catch(() => { _loaderPromise = null; });
    return _loaderPromise;
}

function _tryLoadChain(chains, index) {
    return new Promise((resolve, reject) => {
        if (index >= chains.length) { reject(new Error('All Monaco CDN sources failed.')); return; }
        if (window.monaco?.editor) { resolve(window.monaco); return; }
        const { loader: loaderUrl, vs: vsPath } = chains[index];
        const tagId = `sg-monaco-loader-${index}`;
        const existing = document.getElementById(tagId);
        if (existing) {
            if (window.require?.config) _finalizeLoader(vsPath, resolve, reject, () => _tryLoadChain(chains, index + 1).then(resolve, reject));
            else existing.addEventListener('load', () => _finalizeLoader(vsPath, resolve, reject, () => _tryLoadChain(chains, index + 1).then(resolve, reject)), { once: true });
            return;
        }
        const s = document.createElement('script');
        s.src = loaderUrl; s.async = true; s.id = tagId;
        s.onload = () => _finalizeLoader(vsPath, resolve, reject, () => _tryLoadChain(chains, index + 1).then(resolve, reject));
        s.onerror = () => _tryLoadChain(chains, index + 1).then(resolve, reject);
        document.head.appendChild(s);
    });
}

function _finalizeLoader(vsPath, resolve, reject, fallback) {
    try {
        window.require.config({ paths: { vs: vsPath } });
        window.require(['vs/editor/editor.main'], () => {
            if (window.monaco?.editor) { _registerCustomThemes(window.monaco); resolve(window.monaco); }
            else fallback();
        }, () => fallback());
    } catch { fallback(); }
}

function _registerCustomThemes(monaco) {
    if (monaco._sgThemesReady) return;
    try {
        monaco.editor.defineTheme('sg-light', {
            base: 'vs', inherit: true, rules: [], colors: {
                'editor.background': '#ffffff', 'editor.foreground': '#0f172a',
                'editorLineNumber.foreground': '#94a3b8', 'editorLineNumber.activeForeground': '#2563eb',
                'editor.lineHighlightBackground': '#f8fafc', 'editorCursor.foreground': '#2563eb',
                'editor.selectionBackground': '#dbeafe', 'editor.inactiveSelectionBackground': '#e2e8f0',
                'editorIndentGuide.background': '#e2e8f0', 'editorIndentGuide.activeBackground': '#cbd5e1',
                'editorBracketMatch.background': '#dbeafe', 'editorBracketMatch.border': '#2563eb',
                'editorWidget.background': '#ffffff', 'editorWidget.border': '#e2e8f0',
                'scrollbarSlider.background': 'rgba(15,23,42,0.16)', 'scrollbarSlider.hoverBackground': 'rgba(15,23,42,0.28)', 'scrollbarSlider.activeBackground': 'rgba(15,23,42,0.36)',
            }
        });
        monaco.editor.defineTheme('sg-dark', {
            base: 'vs-dark', inherit: true, rules: [], colors: {
                'editor.background': '#0f172a', 'editor.foreground': '#f1f5f9',
                'editorLineNumber.foreground': '#475569', 'editorLineNumber.activeForeground': '#60a5fa',
                'editor.lineHighlightBackground': '#1e293b', 'editorCursor.foreground': '#60a5fa',
                'editor.selectionBackground': 'rgba(96,165,250,0.25)', 'editor.inactiveSelectionBackground': '#1e293b',
                'editorIndentGuide.background': '#1e293b', 'editorIndentGuide.activeBackground': '#334155',
                'editorBracketMatch.background': 'rgba(96,165,250,0.18)', 'editorBracketMatch.border': '#60a5fa',
                'editorWidget.background': '#0f172a', 'editorWidget.border': '#1e293b',
                'scrollbarSlider.background': 'rgba(255,255,255,0.10)', 'scrollbarSlider.hoverBackground': 'rgba(255,255,255,0.18)', 'scrollbarSlider.activeBackground': 'rgba(255,255,255,0.26)',
            }
        });
        monaco._sgThemesReady = true;
    } catch { /* best-effort */ }
}

function _detectSgTheme() {
    try {
        const html = document.documentElement;
        const mode = html.getAttribute('data-theme');
        if (mode === 'dark') return 'sg-dark';
        if (mode === 'light') return 'sg-light';
        return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'sg-dark' : 'sg-light';
    } catch { return 'sg-light'; }
}

function _wireThemeObserver(monaco) {
    if (_themeObserver) return;
    _themeObserver = new MutationObserver(() => {
        const t = _detectSgTheme();
        try { monaco.editor.setTheme(t); } catch { }
    });
    _themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
}

function _editorOrDefault(inst) {
    return inst._isDiffEditor ? inst.editor.getModifiedEditor() : inst.editor;
}

function _buildMonacoOpts(opts) {
    const theme = (!opts?.theme || opts.theme === 'sg-auto') ? _detectSgTheme() : opts.theme;
    return {
        value: '',
        language: opts?.language ?? 'json',
        theme,
        fontSize: opts?.fontSize ?? 13,
        readOnly: opts?.readOnly ?? false,
        minimap: { enabled: opts?.minimap ?? false },
        lineNumbers: opts?.lineNumbers !== false ? 'on' : 'off',
        wordWrap: opts?.wordWrap ? 'on' : 'off',
        automaticLayout: true,
        scrollBeyondLastLine: false,
        smoothScrolling: true,
        cursorBlinking: opts?.cursorBlinking ?? 'smooth',
        cursorSmoothCaretAnimation: 'on',
        cursorStyle: opts?.cursorStyle ?? 'line',
        renderLineHighlight: opts?.renderLineHighlight ?? 'line',
        renderWhitespace: opts?.renderWhitespace ?? 'selection',
        roundedSelection: false,
        padding: { top: opts?.paddingTop ?? 10, bottom: opts?.paddingBottom ?? 10 },
        fontFamily: opts?.fontFamily || "'JetBrains Mono','Fira Code','Cascadia Code',ui-monospace,SFMono-Regular,Consolas,monospace",
        fontLigatures: opts?.fontLigatures !== false,
        tabSize: opts?.tabSize ?? 2,
        insertSpaces: true,
        formatOnPaste: opts?.formatOnPaste !== false,
        formatOnType: false,
        bracketPairColorization: { enabled: opts?.bracketPairColorization !== false },
        guides: { bracketPairs: 'active', indentation: true },
        occurrencesHighlight: opts?.occurrencesHighlight !== false ? 'singleFile' : 'off',
        selectionHighlight: opts?.selectionHighlight !== false,
        folding: opts?.folding !== false,
        foldingHighlight: true,
        codeLens: opts?.codeLens !== false,
        colorDecorators: opts?.colorDecorators !== false,
        links: opts?.links !== false,
        suggest: { quickSuggestions: opts?.quickSuggestions !== false, snippetSuggestions: 'inline', parameterHints: { enabled: opts?.parameterHints !== false } },
        stickyScroll: { enabled: opts?.stickyScroll === true },
        scrollbar: { useShadows: false, verticalScrollbarSize: 10, horizontalScrollbarSize: 10 },
        overviewRulerLanes: 0,
        overviewRulerBorder: false,
        hideCursorInOverviewRuler: true,
        ...(opts?.minHeight != null && { minHeight: opts.minHeight }),
        ...(opts?.maxHeight != null && { maxHeight: opts.maxHeight }),
    };
}

// ── Lifecycle ─────────────────────────────────────────────────────────────────

export async function initEditor(dotnetRef, containerRef, instanceId, opts, initialValue, sources) {
    await disposeEditor(instanceId);
    if (!containerRef || !(containerRef instanceof HTMLElement) || !containerRef.isConnected)
        throw new Error('Container element is not connected to the DOM.');

    const monaco = await _loadMonaco(sources);
    const editorOpts = _buildMonacoOpts(opts);
    editorOpts.value = initialValue ?? '';

    const editor = monaco.editor.create(containerRef, editorOpts);

    const trackSgTheme = !opts?.theme || opts.theme === 'sg-auto';
    if (trackSgTheme) _wireThemeObserver(monaco);

    if ((opts?.language === 'json' || !opts?.language) && opts?.autoFormat !== false) {
        setTimeout(() => { try { editor.getAction('editor.action.formatDocument')?.run(); } catch { } }, 250);
    }

    const subs = [];
    let debounce = null;
    subs.push(editor.onDidChangeModelContent(() => {
        const inst = _instances.get(instanceId);
        if (!inst || inst._settingValue) return;
        clearTimeout(debounce);
        debounce = setTimeout(() => {
            try { dotnetRef.invokeMethodAsync('OnValueChangedAsync', editor.getValue()); } catch { }
        }, 250);
    }));

    subs.push(editor.onDidChangeCursorPosition(e => {
        try { dotnetRef.invokeMethodAsync('OnCursorPositionChangedAsync', e.position.lineNumber, e.position.column); } catch { }
    }));

    subs.push(editor.onDidFocusEditorText(() => {
        try { dotnetRef.invokeMethodAsync('OnFocusAsync'); } catch { }
    }));

    subs.push(editor.onDidBlurEditorText(() => {
        try { dotnetRef.invokeMethodAsync('OnBlurAsync'); } catch { }
    }));

    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { editor.layout(); } catch { } });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { editor, monaco, dotnetRef, ro, subs, _settingValue: false, _debounce: debounce });
}

export async function createDiffEditor(dotnetRef, containerRef, instanceId, originalValue, modifiedValue, opts, sources) {
    await disposeEditor(instanceId);
    if (!containerRef || !(containerRef instanceof HTMLElement) || !containerRef.isConnected)
        throw new Error('Container element is not connected to the DOM.');

    const monaco = await _loadMonaco(sources);
    const lang = opts?.language ?? 'json';
    const originalModel = monaco.editor.createModel(originalValue ?? '', lang);
    const modifiedModel = monaco.editor.createModel(modifiedValue ?? '', lang);
    const editorOpts = _buildMonacoOpts(opts);

    const diffEditor = monaco.editor.createDiffEditor(containerRef, editorOpts);
    diffEditor.setModel({ original: originalModel, modified: modifiedModel });

    const trackSgTheme = !opts?.theme || opts.theme === 'sg-auto';
    if (trackSgTheme) _wireThemeObserver(monaco);

    const subs = [];
    let debounce = null;
    const modifiedEditor = diffEditor.getModifiedEditor();
    subs.push(modifiedEditor.onDidChangeModelContent(() => {
        const inst = _instances.get(instanceId);
        if (!inst || inst._settingValue) return;
        clearTimeout(debounce);
        debounce = setTimeout(() => {
            try { dotnetRef.invokeMethodAsync('OnValueChangedAsync', modifiedEditor.getValue()); } catch { }
        }, 250);
    }));

    subs.push(modifiedEditor.onDidChangeCursorPosition(e => {
        try { dotnetRef.invokeMethodAsync('OnCursorPositionChangedAsync', e.position.lineNumber, e.position.column); } catch { }
    }));

    subs.push(modifiedEditor.onDidFocusEditorText(() => {
        try { dotnetRef.invokeMethodAsync('OnFocusAsync'); } catch { }
    }));

    subs.push(modifiedEditor.onDidBlurEditorText(() => {
        try { dotnetRef.invokeMethodAsync('OnBlurAsync'); } catch { }
    }));

    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        ro = new ResizeObserver(() => { try { diffEditor.layout(); } catch { } });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, {
        editor: diffEditor, monaco, dotnetRef, ro, subs,
        _settingValue: false, _debounce: debounce,
        _isDiffEditor: true, _originalModel: originalModel, _modifiedModel: modifiedModel,
    });
}

// ── Commands ──────────────────────────────────────────────────────────────────

export function setupMonacoSaveHandler(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst._saveDisposable?.dispose();
    const target = _editorOrDefault(inst);
    inst._saveDisposable = target.addCommand(inst.monaco.KeyMod.CtrlCmd | inst.monaco.KeyCode.KeyS, () => {
        try { inst.dotnetRef.invokeMethodAsync('OnSaveAsync', target.getValue()); } catch { }
    });
}

export function setupMonacoFormatKeybinding(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst._formatDisposable?.dispose();
    const target = _editorOrDefault(inst);
    inst._formatDisposable = target.addCommand(inst.monaco.KeyMod.Shift | inst.monaco.KeyMod.Alt | inst.monaco.KeyCode.KeyF, () => {
        try { target.getAction('editor.action.formatDocument')?.run(); } catch { }
    });
}

export function setMonacoMarkers(instanceId, markersJson) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const model = inst._isDiffEditor ? inst._modifiedModel : inst.editor.getModel();
    if (!model) return;
    if (!markersJson || markersJson === '[]' || markersJson === '') {
        inst.monaco.editor.setModelMarkers(model, 'sg-monaco', []);
        return;
    }
    let markers;
    try { markers = JSON.parse(markersJson); } catch { return; }
    if (!Array.isArray(markers)) return;
    const severityValues = [inst.monaco.MarkerSeverity.Hint, inst.monaco.MarkerSeverity.Info, inst.monaco.MarkerSeverity.Warning, inst.monaco.MarkerSeverity.Error];
    inst.monaco.editor.setModelMarkers(model, 'sg-monaco', markers.map(m => ({
        severity: severityValues[m.severity] ?? inst.monaco.MarkerSeverity.Info,
        startLineNumber: m.line, startColumn: m.column,
        endLineNumber: m.line, endColumn: m.column + 1,
        message: m.message || '',
    })));
}

export function getValue(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return '';
    if (inst._isDiffEditor) return inst._modifiedModel?.getValue() ?? '';
    return inst.editor.getValue() ?? '';
}

export function setValue(instanceId, value) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const next = value ?? '';
    const model = inst._isDiffEditor ? inst._modifiedModel : inst.editor.getModel();
    if (!model || model.getValue() === next) return;
    const wasReadOnly = !inst._isDiffEditor && inst.editor.getOption(inst.monaco.editor.EditorOption.readOnly);
    if (wasReadOnly) inst.editor.updateOptions({ readOnly: false });
    inst._settingValue = true;
    try { model.applyEdits([{ range: model.getFullModelRange(), text: next, forceMoveMarkers: true }]); }
    finally { inst._settingValue = false; }
    if (wasReadOnly) inst.editor.updateOptions({ readOnly: true });
    if (model.getLanguageId() === 'json') setTimeout(() => { try { _editorOrDefault(inst)?.getAction('editor.action.formatDocument')?.run(); } catch { } }, 100);
}

export function setLanguage(instanceId, language) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst._isDiffEditor) {
        if (inst._originalModel) inst.monaco.editor.setModelLanguage(inst._originalModel, language);
        if (inst._modifiedModel) inst.monaco.editor.setModelLanguage(inst._modifiedModel, language);
    } else {
        const model = inst.editor.getModel();
        if (model) inst.monaco.editor.setModelLanguage(model, language);
    }
}

export function setTheme(instanceId, theme) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.monaco.editor.setTheme((!theme || theme === 'sg-auto') ? _detectSgTheme() : theme);
}

export function format(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { _editorOrDefault(inst).getAction('editor.action.formatDocument')?.run(); } catch { }
}

export function updateEditorOptions(instanceId, partialOpts) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const target = _editorOrDefault(inst);
    const tgtOpts = {};
    if (partialOpts.readOnly != null) tgtOpts.readOnly = partialOpts.readOnly;
    if (partialOpts.fontSize != null) tgtOpts.fontSize = partialOpts.fontSize;
    if (partialOpts.minimap != null) tgtOpts.minimap = { enabled: partialOpts.minimap };
    if (partialOpts.lineNumbers != null) tgtOpts.lineNumbers = partialOpts.lineNumbers ? 'on' : 'off';
    if (partialOpts.wordWrap != null) tgtOpts.wordWrap = partialOpts.wordWrap ? 'on' : 'off';
    if (partialOpts.fontFamily != null) tgtOpts.fontFamily = partialOpts.fontFamily;
    if (partialOpts.fontLigatures != null) tgtOpts.fontLigatures = partialOpts.fontLigatures;
    if (partialOpts.tabSize != null) tgtOpts.tabSize = partialOpts.tabSize;
    if (partialOpts.cursorStyle != null) tgtOpts.cursorStyle = partialOpts.cursorStyle;
    if (partialOpts.cursorBlinking != null) tgtOpts.cursorBlinking = partialOpts.cursorBlinking;
    if (partialOpts.folding != null) tgtOpts.folding = partialOpts.folding;
    if (partialOpts.codeLens != null) tgtOpts.codeLens = partialOpts.codeLens;
    if (partialOpts.quickSuggestions != null) tgtOpts['suggest'] = { quickSuggestions: partialOpts.quickSuggestions };
    if (partialOpts.parameterHints != null) tgtOpts['suggest'] = { ...tgtOpts['suggest'], parameterHints: { enabled: partialOpts.parameterHints } };
    if (partialOpts.bracketPairColorization != null) tgtOpts.bracketPairColorization = { enabled: partialOpts.bracketPairColorization };
    if (partialOpts.paddingTop != null || partialOpts.paddingBottom != null)
        tgtOpts.padding = { top: partialOpts.paddingTop ?? inst._lastPadding?.top ?? 10, bottom: partialOpts.paddingBottom ?? inst._lastPadding?.bottom ?? 10 };
    if (Object.keys(tgtOpts).length) {
        inst._lastPadding = { top: tgtOpts.padding?.top ?? inst._lastPadding?.top ?? 10, bottom: tgtOpts.padding?.bottom ?? inst._lastPadding?.bottom ?? 10 };
        target.updateOptions(tgtOpts);
    }
    if (partialOpts.language != null) setLanguage(instanceId, partialOpts.language);
    if (partialOpts.theme != null) setTheme(instanceId, partialOpts.theme);
}

export function setReadOnly(instanceId, readOnly) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst._isDiffEditor) inst.editor.getModifiedEditor().updateOptions({ readOnly });
    else inst.editor.updateOptions({ readOnly });
}

export function setFontSize(instanceId, size) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst._isDiffEditor) {
        inst.editor.getModifiedEditor().updateOptions({ fontSize: size });
        inst.editor.getOriginalEditor().updateOptions({ fontSize: size });
    } else {
        inst.editor.updateOptions({ fontSize: size });
    }
}

export function getCursorPosition(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return null;
    const pos = _editorOrDefault(inst).getPosition();
    return pos ? { lineNumber: pos.lineNumber, column: pos.column } : null;
}

export function setCursorPosition(instanceId, lineNumber, column) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    _editorOrDefault(inst).setPosition({ lineNumber, column });
    _editorOrDefault(inst).revealPositionInCenter({ lineNumber, column });
}

export function layout(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.editor.layout(); } catch { }
}

export function focus(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { _editorOrDefault(inst).focus(); } catch { }
}

export function disposeEditor(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst._saveDisposable?.dispose(); } catch { }
    try { inst._formatDisposable?.dispose(); } catch { }
    try { inst.ro?.disconnect(); } catch { }
    if (inst.subs) { for (const s of inst.subs) try { s.dispose(); } catch { } }
    if (inst._isDiffEditor) {
        try { inst._originalModel?.dispose(); } catch { }
        try { inst._modifiedModel?.dispose(); } catch { }
    }
    try { inst.editor.dispose(); } catch { }
    _instances.delete(instanceId);
}
