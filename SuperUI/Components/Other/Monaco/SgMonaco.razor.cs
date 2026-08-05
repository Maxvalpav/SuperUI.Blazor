using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;
using System.Text.Json;

namespace SuperUI.Components;

public partial class SgMonaco : SgJsComponentBase
{
    private ElementReference _containerRef;
    private bool _ready;
    private string? _error;
    private string? _lastExternalValue;
    private bool _suppressNextChange;
    private bool _isDiffEditor;
    private IReadOnlyList<SgMonacoMarker>? _prevMarkers;
    private readonly SgMonacoOptions _prevOptions = new();
    private Timer? _autoSaveTimer;

    protected override string ModulePath => "./_content/SuperUI/sg-monaco.js";
    protected override string IdPrefix => "sg-monaco";

    // ── Core Parameters ─────────────────────────────────────────────────────

    /// <summary>Current editor value (two-way binding).</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Fires when the editor content changes.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Monaco editor options.</summary>
    [Parameter] public SgMonacoOptions? Options { get; set; }

    /// <summary>CDN source overrides.</summary>
    [Parameter] public SgMonacoSources? Sources { get; set; }

    /// <summary>Container height. Default "300px".</summary>
    [Parameter] public string Height { get; set; } = "300px";

    /// <summary>Container width. Default "100%".</summary>
    [Parameter] public string Width { get; set; } = "100%";

    // ── Save ────────────────────────────────────────────────────────────────

    /// <summary>Fires when the user presses Ctrl+S (or Cmd+S on Mac).</summary>
    [Parameter] public EventCallback<string?> OnSave { get; set; }

    // ── Format ──────────────────────────────────────────────────────────────

    /// <summary>When true, registers Shift+Alt+F keyboard shortcut and enables FormatAsync. Default false.</summary>
    [Parameter] public bool FormatEnabled { get; set; }

    // ── Markers ─────────────────────────────────────────────────────────────

    /// <summary>Diagnostic markers displayed as squiggly underlines.</summary>
    [Parameter] public IReadOnlyList<SgMonacoMarker>? Markers { get; set; }

    // ── Badge ───────────────────────────────────────────────────────────────

    /// <summary>Shows "Read Only" badge in read-only mode. Default true.</summary>
    [Parameter] public bool ReadOnlyBadge { get; set; } = true;

    // ── AutoSave ────────────────────────────────────────────────────────────

    /// <summary>Auto-saves after a debounce period following the last edit.</summary>
    [Parameter] public bool AutoSave { get; set; }

    /// <summary>Debounce delay in milliseconds for auto-save. Default 2000.</summary>
    [Parameter] public int AutoSaveDelay { get; set; } = 2000;

    /// <summary>Fires when auto-save triggers after a debounced pause.</summary>
    [Parameter] public EventCallback<string?> OnAutoSave { get; set; }

    // ── Sizing ──────────────────────────────────────────────────────────────

    /// <summary>Minimum editor container height (CSS value, e.g. "200px").</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Maximum editor container height (CSS value, e.g. "800px").</summary>
    [Parameter] public string? MaxHeight { get; set; }

    // ── Diff ────────────────────────────────────────────────────────────────

    /// <summary>When set, renders as a diff editor comparing this original value against Value.</summary>
    [Parameter] public string? DiffValue { get; set; }

    // ── Events ──────────────────────────────────────────────────────────────

    /// <summary>Fires once after the editor is fully initialized and ready.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>Fires when the cursor position changes.</summary>
    [Parameter] public EventCallback<SgMonacoCursorPosition> OnCursorPositionChanged { get; set; }

    /// <summary>Fires when the editor gains focus.</summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>Fires when the editor loses focus.</summary>
    [Parameter] public EventCallback OnBlur { get; set; }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        if (!_ready || !IsInteractive) return;

        if (!string.Equals(Value, _lastExternalValue, StringComparison.Ordinal))
        {
            _lastExternalValue = Value;
            _suppressNextChange = true;
            await SafeInvokeVoidAsync("setValue", ResolvedId, Value ?? "");
        }

        if (!ReferenceEquals(Markers, _prevMarkers))
        {
            _prevMarkers = Markers;
            await SyncMarkersAsync();
        }

        var o = Options;
        if (o is not null)
        {
            var changed = new Dictionary<string, object?>();
            if (o.Language != _prevOptions.Language) { changed["language"] = o.Language; _prevOptions.Language = o.Language; }
            if (o.Theme != _prevOptions.Theme) { changed["theme"] = o.Theme; _prevOptions.Theme = o.Theme; }
            if (o.FontSize != _prevOptions.FontSize) { changed["fontSize"] = o.FontSize; _prevOptions.FontSize = o.FontSize; }
            if (o.ReadOnly != _prevOptions.ReadOnly) { changed["readOnly"] = o.ReadOnly; _prevOptions.ReadOnly = o.ReadOnly; }
            if (o.Minimap != _prevOptions.Minimap) { changed["minimap"] = o.Minimap; _prevOptions.Minimap = o.Minimap; }
            if (o.LineNumbers != _prevOptions.LineNumbers) { changed["lineNumbers"] = o.LineNumbers; _prevOptions.LineNumbers = o.LineNumbers; }
            if (o.WordWrap != _prevOptions.WordWrap) { changed["wordWrap"] = o.WordWrap; _prevOptions.WordWrap = o.WordWrap; }
            if (o.FontFamily != _prevOptions.FontFamily) { changed["fontFamily"] = o.FontFamily; _prevOptions.FontFamily = o.FontFamily; }
            if (o.FontLigatures != _prevOptions.FontLigatures) { changed["fontLigatures"] = o.FontLigatures; _prevOptions.FontLigatures = o.FontLigatures; }
            if (o.TabSize != _prevOptions.TabSize) { changed["tabSize"] = o.TabSize; _prevOptions.TabSize = o.TabSize; }
            if (o.CursorStyle != _prevOptions.CursorStyle) { changed["cursorStyle"] = o.CursorStyle; _prevOptions.CursorStyle = o.CursorStyle; }
            if (o.CursorBlinking != _prevOptions.CursorBlinking) { changed["cursorBlinking"] = o.CursorBlinking; _prevOptions.CursorBlinking = o.CursorBlinking; }
            if (o.Folding != _prevOptions.Folding) { changed["folding"] = o.Folding; _prevOptions.Folding = o.Folding; }
            if (o.CodeLens != _prevOptions.CodeLens) { changed["codeLens"] = o.CodeLens; _prevOptions.CodeLens = o.CodeLens; }
            if (o.QuickSuggestions != _prevOptions.QuickSuggestions) { changed["quickSuggestions"] = o.QuickSuggestions; _prevOptions.QuickSuggestions = o.QuickSuggestions; }
            if (o.ParameterHints != _prevOptions.ParameterHints) { changed["parameterHints"] = o.ParameterHints; _prevOptions.ParameterHints = o.ParameterHints; }
            if (o.BracketPairColorization != _prevOptions.BracketPairColorization) { changed["bracketPairColorization"] = o.BracketPairColorization; _prevOptions.BracketPairColorization = o.BracketPairColorization; }

            if (changed.Count > 0)
                await SafeInvokeVoidAsync("updateEditorOptions", ResolvedId, changed);
        }
    }

    protected override async ValueTask OnInteractiveAsync()
    {
        try
        {
            await InitAsync();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load Monaco Editor: {ex.Message}";
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override async ValueTask OnJsInitializationFailedAsync(Exception exception)
    {
        _error = $"Failed to load editor module: {exception.Message}";
        await InvokeAsync(StateHasChanged);
    }

    private async Task InitAsync()
    {
        if (Module is null)
        {
            _error = "JS module not loaded. Refresh the page.";
            await InvokeAsync(StateHasChanged);
            return;
        }

        var opts = BuildOpts();
        var sources = Sources ?? new SgMonacoSources();
        _lastExternalValue = Value;

        if (!string.IsNullOrEmpty(DiffValue))
        {
            _isDiffEditor = true;
            await SafeInvokeVoidAsync("createDiffEditor",
                SelfRef, _containerRef, ResolvedId,
                DiffValue, Value ?? "", opts, sources);
        }
        else
        {
            _isDiffEditor = false;
            await SafeInvokeVoidAsync("initEditor",
                SelfRef, _containerRef, ResolvedId,
                opts, Value ?? "", sources);
        }

        _ready = true;
        _prevMarkers = Markers;
        await InvokeAsync(StateHasChanged);

        if (OnSave.HasDelegate)
            await SafeInvokeVoidAsync("setupMonacoSaveHandler", ResolvedId);

        if (FormatEnabled)
            await SafeInvokeVoidAsync("setupMonacoFormatKeybinding", ResolvedId);

        await SyncMarkersAsync();

        if (OnReady.HasDelegate)
            await OnReady.InvokeAsync();
    }

    private object BuildOpts()
    {
        var o = Options ?? new SgMonacoOptions();
        return new
        {
            language = o.Language,
            theme = o.Theme,
            fontSize = o.FontSize,
            readOnly = o.ReadOnly,
            minimap = o.Minimap,
            lineNumbers = o.LineNumbers,
            wordWrap = o.WordWrap,
            autoFormat = o.AutoFormat,
            fontFamily = o.FontFamily,
            fontLigatures = o.FontLigatures,
            tabSize = o.TabSize,
            minHeight = o.MinHeight,
            maxHeight = o.MaxHeight,
            occurrencesHighlight = o.OccurrencesHighlight,
            selectionHighlight = o.SelectionHighlight,
            folding = o.Folding,
            codeLens = o.CodeLens,
            colorDecorators = o.ColorDecorators,
            links = o.Links,
            quickSuggestions = o.QuickSuggestions,
            parameterHints = o.ParameterHints,
            paddingTop = o.PaddingTop,
            paddingBottom = o.PaddingBottom,
            renderWhitespace = o.RenderWhitespace,
            bracketPairColorization = o.BracketPairColorization,
            cursorStyle = o.CursorStyle,
            cursorBlinking = o.CursorBlinking,
            stickyScroll = o.StickyScroll,
            formatOnPaste = o.FormatOnPaste,
        };
    }

    private async Task SyncMarkersAsync()
    {
        if (!_ready) return;

        if (Markers is null || Markers.Count == 0)
        {
            await SafeInvokeVoidAsync("setMonacoMarkers", ResolvedId, "");
            return;
        }

        var json = JsonSerializer.Serialize(
            Markers.Select(m => new
            {
                line = m.Line,
                column = m.Column,
                message = m.Message,
                severity = (int)m.Severity
            }),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await SafeInvokeVoidAsync("setMonacoMarkers", ResolvedId, json);
    }

    // ── Public methods ──────────────────────────────────────────────────────

    /// <summary>Formats the document using Monaco's format action.</summary>
    public async Task FormatAsync() =>
        await SafeInvokeVoidAsync("format", ResolvedId);

    /// <summary>Sets the editor language.</summary>
    public async Task SetLanguageAsync(string language) =>
        await SafeInvokeVoidAsync("setLanguage", ResolvedId, language);

    /// <summary>Sets the editor theme.</summary>
    public async Task SetThemeAsync(string theme) =>
        await SafeInvokeVoidAsync("setTheme", ResolvedId, theme);

    /// <summary>Sets the editor font size.</summary>
    public async Task SetFontSizeAsync(int fontSize) =>
        await SafeInvokeVoidAsync("setFontSize", ResolvedId, fontSize);

    /// <summary>Gets the current editor value.</summary>
    public async Task<string> GetValueAsync()
    {
        if (!IsInteractive) return Value ?? "";
        try { return await SafeInvokeAsync<string>("getValue", ResolvedId) ?? ""; }
        catch (JSDisconnectedException) { return Value ?? ""; }
        catch (TaskCanceledException) { return Value ?? ""; }
        catch (ObjectDisposedException) { return Value ?? ""; }
    }

    /// <summary>Sets the editor content programmatically.</summary>
    public async Task SetValueAsync(string value)
    {
        if (!_ready || !IsInteractive) { Value = value; return; }
        _lastExternalValue = value;
        _suppressNextChange = true;
        await SafeInvokeVoidAsync("setValue", ResolvedId, value ?? "");
    }

    /// <summary>Sets the editor read-only state.</summary>
    public async Task SetReadOnlyAsync(bool readOnly) =>
        await SafeInvokeVoidAsync("setReadOnly", ResolvedId, readOnly);

    /// <summary>Gets the current cursor position.</summary>
    public async Task<SgMonacoCursorPosition?> GetCursorPositionAsync()
    {
        if (!IsInteractive) return null;
        try { return await SafeInvokeAsync<SgMonacoCursorPosition>("getCursorPosition", ResolvedId); }
        catch (JSDisconnectedException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (ObjectDisposedException) { return null; }
    }

    /// <summary>Sets the cursor position.</summary>
    public async Task SetCursorPositionAsync(int lineNumber, int column) =>
        await SafeInvokeVoidAsync("setCursorPosition", ResolvedId, lineNumber, column);

    /// <summary>Focuses the editor.</summary>
    public async Task FocusAsync() =>
        await SafeInvokeVoidAsync("focus", ResolvedId);

    /// <summary>Triggers editor layout recalculation.</summary>
    public async Task LayoutAsync() =>
        await SafeInvokeVoidAsync("layout", ResolvedId);

    /// <summary>Retries loading after an error.</summary>
    public async Task RetryAsync()
    {
        if (Module is null)
        {
            await JS.InvokeVoidAsync("location.reload");
            return;
        }
        _error = null;
        _ready = false;
        await InvokeAsync(StateHasChanged);
        await InitAsync();
    }

    // ── JS callbacks ────────────────────────────────────────────────────────

    [JSInvokable]
    public async Task OnValueChangedAsync(string value)
    {
        if (_suppressNextChange)
        {
            _suppressNextChange = false;
            return;
        }
        _lastExternalValue = value;
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(value);

        if (AutoSave)
        {
            _autoSaveTimer?.Dispose();
            var captured = value;
            _autoSaveTimer = new Timer(async _ =>
            {
                try
                {
                    await InvokeAsync(async () =>
                    {
                        if (!IsDisposed && OnAutoSave.HasDelegate)
                            await OnAutoSave.InvokeAsync(captured);
                    });
                }
                catch (TaskCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }, null, AutoSaveDelay, Timeout.Infinite);
        }
    }

    [JSInvokable]
    public async Task OnSaveAsync(string value)
    {
        if (OnSave.HasDelegate)
            await OnSave.InvokeAsync(value);
    }

    [JSInvokable]
    public async Task OnCursorPositionChangedAsync(int lineNumber, int column)
    {
        if (OnCursorPositionChanged.HasDelegate)
            await OnCursorPositionChanged.InvokeAsync(new SgMonacoCursorPosition
            {
                LineNumber = lineNumber,
                Column = column
            });
    }

    [JSInvokable]
    public async Task OnFocusAsync()
    {
        if (OnFocus.HasDelegate)
            await OnFocus.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnBlurAsync()
    {
        if (OnBlur.HasDelegate)
            await OnBlur.InvokeAsync();
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    protected override async ValueTask OnDisposingAsync()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
        await SafeInvokeVoidAsync("disposeEditor", ResolvedId);
    }
}
