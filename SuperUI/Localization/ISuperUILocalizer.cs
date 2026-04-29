namespace SuperUI.Localization;

/// <summary>
/// Provides localized strings for SuperUI components.
/// </summary>
public interface ISuperUILocalizer
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string this[string key] { get; }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    string GetString(string key, params object[] args);
}
