using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Manages theme (light/dark mode) for SuperUI components.
/// </summary>
public sealed class SgThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private bool _isDisposed;

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event Action<string>? ThemeChanged;

    /// <summary>
    /// Gets the current theme: "light", "dark", or "auto".
    /// </summary>
    public string CurrentTheme { get; private set; } = "auto";

    /// <summary>
    /// Initializes a new instance of <see cref="SgThemeService"/>.
    /// </summary>
    public SgThemeService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Initializes the theme service and loads the saved theme preference.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isDisposed) return;

        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-theme.js");
            CurrentTheme = await _module.InvokeAsync<string>("getTheme");
        }
        catch (JSException) { }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Sets the theme to "light", "dark", or "auto".
    /// </summary>
    /// <param name="theme">The theme to set.</param>
    public async Task SetThemeAsync(string theme)
    {
        if (_isDisposed || _module is null) return;

        try
        {
            await _module.InvokeVoidAsync("setTheme", theme);
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
        catch (JSException) { }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        var newTheme = CurrentTheme == "dark" ? "light" : "dark";
        await SetThemeAsync(newTheme);
    }

    /// <summary>
    /// Gets the effective theme (resolves "auto" to "light" or "dark").
    /// </summary>
    public async Task<string> GetEffectiveThemeAsync()
    {
        if (_isDisposed || _module is null) return "light";

        try
        {
            return await _module.InvokeAsync<string>("getEffectiveTheme");
        }
        catch
        {
            return "light";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
