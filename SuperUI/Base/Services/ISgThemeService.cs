// SuperUI/Base/Services/ISgThemeService.cs
// ✅ NEW: интерфейс для DI

namespace SuperUI.Base.Services;

public interface ISgThemeService
{
    string CurrentTheme { get; }
    event Action<string>? ThemeChanged;
    Task InitializeAsync();
    Task SetThemeAsync(string theme);
    Task ToggleThemeAsync();
    Task<string> GetEffectiveThemeAsync();
}
