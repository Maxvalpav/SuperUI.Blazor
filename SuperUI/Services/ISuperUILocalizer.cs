namespace SuperUI.Localization;

/// <summary>
/// Provides localized strings for SuperUI components.
/// </summary>
public interface ISuperUILocalizer
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Raised when the current locale changes, allowing components to re-render.
    /// </summary>
    event Action? OnLocaleChanged;

    /// <summary>
    /// Gets the current language code (e.g. "en", "ru").
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Changes the current language and fires <see cref="OnLocaleChanged"/>.
    /// </summary>
    void SetLanguage(string lang);

    /// <summary>
    /// Gets the list of available language codes.
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }
}
