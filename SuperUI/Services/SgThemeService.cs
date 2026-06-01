using Microsoft.JSInterop;
using SuperUI.Themes;

namespace SuperUI.Services;

/// <summary>
/// Extended theme management service for SuperUI.
/// Supports multiple themes and light/dark modes.
///
/// 2.0-rc3 (PR #5, B1) — debounce + batch:
/// - All state-mutating calls go through a 150 ms debounce; the trailing
///   call persists state in one round-trip via
///   <c>SuperUI.applyThemeState({...})</c> instead of 5 separate
///   <c>localStorage.setItem</c> + 6 <c>eval</c> calls per change.
/// - <see cref="InitializeAsync"/> is idempotent: re-entering it is a no-op
///   while the module is already loaded, and is a no-op after disposal.
/// - <see cref="DisposeAsync"/> captures the <see cref="ThemeChanged"/>
///   delegate locally before nulling it, so any in-flight invocation
///   finishes against a stable handler list (prevents the "collection
///   modified during iteration" race that the previous null-out caused
///   when subscribers fired during teardown).
/// </summary>
public sealed class SgThemeService : IAsyncDisposable
{
    private const string StorageKeyThemeId   = "superui-theme-id";
    private const string StorageKeyDarkMode  = "superui-dark-mode";
    private const string StorageKeyFontSize  = "superui-font-size";
    private const string StorageKeyFontFamily = "superui-font-family";
    private const string StorageKeyDensity   = "superui-density";

    private const int DebounceMilliseconds = 150;

    private readonly IJSRuntime  _js;
    private readonly ThemeRegistry _registry;
    private IJSObjectReference?  _module;
    private bool _isDisposed;
    private bool _isInitialized;

    // Debounce machinery: each new mutation cancels the prior pending flush
    // and schedules a new one. The pending CTS is replaced atomically.
    private CancellationTokenSource? _debounceCts;
    private readonly object _debounceLock = new();

    /// <summary>Current active theme definition.</summary>
    public IThemeDefinition CurrentTheme { get; private set; }

    /// <summary>Current mode: "light" | "dark" | "auto".</summary>
    public string CurrentMode { get; private set; } = "light";

    public string FontSize { get; private set; } = "md";
    public string FontFamily { get; private set; } = "sans";
    public string Density { get; private set; } = "relaxed";

    /// <summary>true if currently in dark mode.</summary>
    public bool IsDark => CurrentMode == "dark" || (CurrentMode == "auto" && _systemPrefersDark);

    private bool _systemPrefersDark;

    /// <summary>Event raised when theme or mode changes.</summary>
    public event Action<IThemeDefinition, string>? ThemeChanged;

    public SgThemeService(IJSRuntime js, ThemeRegistry registry)
    {
        _js = js;
        _registry = registry;
        CurrentTheme = registry.GetDefault();
    }

    /// <summary>Initializes theme service and loads saved preferences.</summary>
    /// <remarks>Idempotent: subsequent calls are no-ops until disposal.</remarks>
    public async Task InitializeAsync()
    {
        if (_isDisposed) return;
        if (_isInitialized) return;
        if (_module is not null) return;

        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-theme.js");

            // Load saved settings in a single JS round-trip (batched).
            var saved = await _module.InvokeAsync<ThemeStateDto>("getSavedState");

            // Determine system preference
            _systemPrefersDark = await _js.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");

            if (saved is not null)
            {
                if (!string.IsNullOrEmpty(saved.ThemeId) && _registry.TryGet(saved.ThemeId, out var theme))
                {
                    CurrentTheme = theme!;
                }
                CurrentMode   = saved.Mode      ?? "light";
                FontSize      = saved.FontSize  ?? "md";
                FontFamily    = saved.FontFamily ?? "sans";
                Density       = saved.Density   ?? "relaxed";
            }

            // Tell the JS module which "auto" mode should subscribe to system
            // changes (B2). Idempotent on the JS side.
            await _module.InvokeVoidAsync("initAutoMode", CurrentMode == "auto");

            _isInitialized = true;
            await ApplyThemeAsync();
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { /* prerender */ }
    }

    public Task SetFontSizeAsync(string size)
    {
        FontSize = size;
        return ScheduleApplyAsync();
    }

    public Task SetFontFamilyAsync(string family)
    {
        FontFamily = family;
        return ScheduleApplyAsync();
    }

    public Task SetDensityAsync(string density)
    {
        Density = density;
        return ScheduleApplyAsync();
    }

    /// <summary>Sets theme by ID.</summary>
    public Task SetThemeAsync(string themeId)
    {
        if (_isDisposed) return Task.CompletedTask;
        if (!_registry.TryGet(themeId, out var theme)) return Task.CompletedTask;

        CurrentTheme = theme!;
        return ScheduleApplyAsync();
    }

    /// <summary>Sets theme by object.</summary>
    public Task SetThemeAsync(IThemeDefinition theme)
    {
        if (_isDisposed) return Task.CompletedTask;
        CurrentTheme = theme;
        return ScheduleApplyAsync();
    }

    /// <summary>Sets mode: "light" | "dark" | "auto".</summary>
    public Task SetModeAsync(string mode)
    {
        if (_isDisposed) return Task.CompletedTask;
        CurrentMode = mode;
        // Re-arm the matchMedia subscription when the mode changes.
        if (_module is not null)
        {
            _ = SafeVoidAsync(_module.InvokeVoidAsync("initAutoMode", mode == "auto"));
        }
        return ScheduleApplyAsync();
    }

    /// <summary>Toggles light ↔ dark.</summary>
    public Task ToggleModeAsync()
    {
        var newMode = IsDark ? "light" : "dark";
        return SetModeAsync(newMode);
    }

    public IReadOnlyList<IThemeDefinition> GetAvailableThemes() => _registry.GetAll();

    /// <summary>
    /// Coalesces rapid mutations into a single apply 150 ms after the last
    /// change. Cancellation is done by replacing the pending CTS; only the
    /// trailing caller actually reaches the JS interop layer.
    /// </summary>
    private Task ScheduleApplyAsync()
    {
        if (_isDisposed) return Task.CompletedTask;
        if (_module is null) return Task.CompletedTask; // not initialized; ignore

        CancellationToken token;
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            token = _debounceCts.Token;
        }

        return DebounceAsync(token);
    }

    private async Task DebounceAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(DebounceMilliseconds, token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer ScheduleApplyAsync call
        }
        catch (ObjectDisposedException)
        {
            return; // service disposed during the wait
        }

        if (_isDisposed || token.IsCancellationRequested) return;

        await ApplyThemeAsync();
    }

    private async Task ApplyThemeAsync()
    {
        if (_isDisposed) return;
        var module = _module;
        if (module is null) return;

        var effectiveDark = IsDark;
        var css = CurrentTheme.GenerateCss();
        var dataTheme = effectiveDark ? "dark" : "light";

        var fontFamilyValue = FontFamily == "mono" ? "mono" : "sans";
        var densityValue = Density == "compact" ? "compact" : "relaxed";
        var fontSizeValue = FontSize switch { "sm" => "sm", "lg" => "lg", _ => "md" };

        // Single JS round-trip: persist + apply.
        try
        {
            await module.InvokeVoidAsync("applyThemeState", new ThemeStateDto
            {
                ThemeId    = CurrentTheme.Id,
                Mode       = CurrentMode,
                FontSize   = FontSize,
                FontFamily = FontFamily,
                Density    = Density,
                Css        = css,
                DataTheme  = dataTheme,
                AttrFontFamily = fontFamilyValue,
                AttrDensity    = densityValue,
                AttrFontSize   = fontSizeValue,
            });
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { /* prerender: JS not yet available */ }

        // Capture the delegate locally before invoking so that a concurrent
        // DisposeAsync that nulls the event cannot race the invocation.
        var handler = ThemeChanged;
        if (handler is not null)
        {
            try { handler(CurrentTheme, CurrentMode); }
            catch { /* подписчик упал — не наш контракт */ }
        }
    }

    private async Task SafeVoidAsync(ValueTask task)
    {
        try { await task; }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Cancel any pending debounce flush.
        CancellationTokenSource? cts;
        lock (_debounceLock)
        {
            cts = _debounceCts;
            _debounceCts = null;
        }
        if (cts is not null)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
            cts.Dispose();
        }

        // Capture the current ThemeChanged delegate locally so subscribers
        // still see a consistent list during the teardown, then null the
        // event so no new subscribers can attach after Dispose.
        var existingHandler = ThemeChanged;
        ThemeChanged = null;

        var module = _module;
        _module = null;
        _isInitialized = false;
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        GC.SuppressFinalize(this);
    }

    // DTO mirrored on the JS side; field names are camelCased by the
    // interop layer so superui-theme.js can read them as state.themeId etc.
    private sealed class ThemeStateDto
    {
        public string? ThemeId    { get; set; }
        public string? Mode       { get; set; }
        public string? FontSize   { get; set; }
        public string? FontFamily { get; set; }
        public string? Density    { get; set; }
        public string  Css        { get; set; } = "";
        public string? DataTheme  { get; set; }
        public string? AttrFontFamily { get; set; }
        public string? AttrDensity    { get; set; }
        public string? AttrFontSize   { get; set; }
    }
}
