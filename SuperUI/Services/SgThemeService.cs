using Microsoft.JSInterop;
using SuperUI.Themes;

namespace SuperUI.Services;

/// <summary>
/// Extended theme management service for SuperUI.
/// Supports multiple themes and light/dark modes.
/// </summary>
public sealed class SgThemeService : IAsyncDisposable
{
    private const string StorageKeyThemeId   = "superui-theme-id";
    private const string StorageKeyDarkMode  = "superui-dark-mode";
    private const string StorageKeyFontSize  = "superui-font-size";
    private const string StorageKeyFontFamily = "superui-font-family";
    private const string StorageKeyDensity   = "superui-density";

    private readonly IJSRuntime  _js;
    private readonly ThemeRegistry _registry;
    private IJSObjectReference?  _module;
    private bool _isDisposed;

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
    public async Task InitializeAsync()
    {
        if (_isDisposed) return;
        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-theme.js");

            // Load saved settings
            var savedThemeId = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKeyThemeId);
            var savedMode = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKeyDarkMode);
            var savedFontSize = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKeyFontSize);
            var savedFontFamily = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKeyFontFamily);
            var savedDensity = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKeyDensity);

            // Determine system preference
            _systemPrefersDark = await _js.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");

            // Apply saved settings
            if (!string.IsNullOrEmpty(savedThemeId) && _registry.TryGet(savedThemeId, out var theme))
            {
                CurrentTheme = theme!;
            }

            CurrentMode = savedMode ?? "light";
            FontSize = savedFontSize ?? "md";
            FontFamily = savedFontFamily ?? "sans";
            Density = savedDensity ?? "relaxed";

            await ApplyThemeAsync();
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException) { }
    }

    public async Task SetFontSizeAsync(string size)
    {
        FontSize = size;
        await SaveAndApplyAsync();
    }

    public async Task SetFontFamilyAsync(string family)
    {
        FontFamily = family;
        await SaveAndApplyAsync();
    }

    public async Task SetDensityAsync(string density)
    {
        Density = density;
        await SaveAndApplyAsync();
    }

    /// <summary>Sets theme by ID.</summary>
    public async Task SetThemeAsync(string themeId)
    {
        if (_isDisposed) return;
        if (!_registry.TryGet(themeId, out var theme)) return;

        CurrentTheme = theme!;
        await SaveAndApplyAsync();
    }

    /// <summary>Sets theme by object.</summary>
    public async Task SetThemeAsync(IThemeDefinition theme)
    {
        if (_isDisposed) return;
        CurrentTheme = theme;
        await SaveAndApplyAsync();
    }

    /// <summary>Sets mode: "light" | "dark" | "auto".</summary>
    public async Task SetModeAsync(string mode)
    {
        if (_isDisposed) return;
        CurrentMode = mode;
        await SaveAndApplyAsync();
    }

    /// <summary>Toggles light ↔ dark.</summary>
    public async Task ToggleModeAsync()
    {
        var newMode = IsDark ? "light" : "dark";
        await SetModeAsync(newMode);
    }

    private async Task SaveAndApplyAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKeyThemeId, CurrentTheme.Id);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKeyDarkMode, CurrentMode);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKeyFontSize, FontSize);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKeyFontFamily, FontFamily);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKeyDensity, Density);
            await ApplyThemeAsync();
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException) { }
    }

    private async Task ApplyThemeAsync()
    {
        if (_isDisposed) return;

        var effectiveDark = IsDark;
        var css = CurrentTheme.GenerateCss();
        var dataTheme = effectiveDark ? "dark" : "light";

        try
        {
            await _js.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{dataTheme}')");
            await _js.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme-id', '{CurrentTheme.Id}')");
            
            // Typography & Density
            var fontSizeValue = FontSize switch { "sm" => "14px", "lg" => "18px", _ => "16px" };
            var fontFamilyValue = FontFamily == "mono" ? "var(--sg-font-mono)" : "var(--sg-font-sans)";
            var densityValue = Density == "compact" ? "0.75" : "1.0";

            await _js.InvokeVoidAsync("eval", $"document.documentElement.style.setProperty('--sg-base-font-size', '{fontSizeValue}')");
            await _js.InvokeVoidAsync("eval", $"document.documentElement.style.setProperty('--sg-base-font-family', '{fontFamilyValue}')");
            await _js.InvokeVoidAsync("eval", $"document.documentElement.style.setProperty('--sg-density-factor', '{densityValue}')");

            await _js.InvokeVoidAsync("SuperUI.applyThemeCss", css);

            ThemeChanged?.Invoke(CurrentTheme, CurrentMode);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException) { }
    }

    public IReadOnlyList<IThemeDefinition> GetAvailableThemes() => _registry.GetAll();

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { }
        }
    }
}
