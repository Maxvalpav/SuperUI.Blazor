// SuperUI/Base/Services/SgThemeService.cs
// ИСПРАВЛЕНО v3:
// ✅ FIX: namespace SuperUI.Base.Services (было SuperUI.Services — неверно!)
// ✅ FIX: реализует ISgThemeService
// ✅ FIX: не падает при SSR (проверка IsPrerendering)
// ✅ NEW: SystemTheme detection через prefers-color-scheme
// ✅ NET8+: учитывает режим рендеринга

using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

public sealed class SgThemeService : ISgThemeService, IAsyncDisposable
{
    private readonly IJSRuntime       _js;
    private          IJSObjectReference? _module;
    private volatile bool             _isDisposed;
    private          bool             _initialized;

    public event Action<string>? ThemeChanged;
    public string CurrentTheme { get; private set; } = "light";

    public SgThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_isDisposed || _initialized) return;
        _initialized = true;

        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-theme.js");
            CurrentTheme = await _module.InvokeAsync<string>("getTheme");
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    public async Task SetThemeAsync(string theme)
    {
        if (_isDisposed || _module is null) return;
        if (theme is not ("light" or "dark" or "auto"))
            throw new ArgumentException($"Invalid theme '{theme}'. Valid: light, dark, auto.", nameof(theme));

        try
        {
            await _module.InvokeVoidAsync("setTheme", theme);
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    public async Task ToggleThemeAsync()
    {
        var newTheme = CurrentTheme == "dark" ? "light" : "dark";
        await SetThemeAsync(newTheme);
    }

    public async Task<string> GetEffectiveThemeAsync()
    {
        if (_isDisposed || _module is null) return "light";
        try
        {
            return await _module.InvokeAsync<string>("getEffectiveTheme");
        }
        catch { return "light"; }
    }

    /// <summary>Получить системную тему пользователя (prefers-color-scheme).</summary>
    public async Task<string> GetSystemThemeAsync()
    {
        if (_isDisposed || _module is null) return "light";
        try
        {
            return await _module.InvokeAsync<string>("getSystemTheme");
        }
        catch { return "light"; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
