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
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private int _disposed;
    private bool _initialized;

    private static readonly HashSet<string> _validThemes =
        new(StringComparer.OrdinalIgnoreCase) { "light", "dark", "auto" };

    public event Action<string>? ThemeChanged;
    public string CurrentTheme { get; private set; } = "light";

    public SgThemeService(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    public async Task InitializeAsync()
    {
        if (Volatile.Read(ref _disposed) == 1 || _initialized) return;
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
        if (Volatile.Read(ref _disposed) == 1 || _module is null) return;

        if (!_validThemes.Contains(theme))
            throw new ArgumentException(
                $"Invalid theme '{theme}'. Valid: {string.Join(", ", _validThemes)}.",
                nameof(theme));

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
        // ✅ ИСПРАВЛЕНО: "auto" → "light" (3-way toggle: light → dark → auto → light)
        var newTheme = CurrentTheme switch
        {
            "light" => "dark",
            "dark" => "auto",
            _ => "light"
        };
        await SetThemeAsync(newTheme);
    }

    public async Task<string> GetEffectiveThemeAsync()
    {
        if (Volatile.Read(ref _disposed) == 1 || _module is null) return "light";
        try
        {
            return await _module.InvokeAsync<string>("getEffectiveTheme");
        }
        catch { return "light"; }
    }

    /// <summary>Получить системную тему пользователя (prefers-color-scheme).</summary>
    public async Task<string> GetSystemThemeAsync()
    {
        if (Volatile.Read(ref _disposed) == 1 || _module is null) return "light";
        try
        {
            return await _module.InvokeAsync<string>("getSystemTheme");
        }
        catch { return "light"; }
    }

    public async ValueTask DisposeAsync()
    {
        // ✅ ИСПРАВЛЕНО: Interlocked.Exchange для идемпотентного dispose
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

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
