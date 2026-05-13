// ================================================================
// Файл: SuperUI/Base/Localization/ISuperUILocalizer.cs
// ДОБАВЛЕНО: метод GetString(string key, string defaultValue)
// ================================================================

using System.Globalization;

namespace SuperUI.Base.Localization;

public interface ISuperUILocalizer
{
    CultureInfo CurrentCulture { get; set; }

    string this[string key] { get; }

    /// <summary>
    /// Get a localized string by key with fallback default value.
    /// </summary>
    string GetString(string key, string defaultValue);

    /// <summary>
    /// Get a localized string by key with format parameters.
    /// </summary>
    string GetString(string key, params object[] args);

    bool TryGetString(string key, out string value);

    string Format(string key, params object[] args);

    event Action<CultureInfo>? CultureChanged;
}
