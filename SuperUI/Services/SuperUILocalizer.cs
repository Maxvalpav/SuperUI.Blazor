using Microsoft.Extensions.Options;

namespace SuperUI.Localization;

/// <summary>
/// Default implementation of <see cref="ISuperUILocalizer"/> using JSON locale files
/// embedded as assembly resources.
/// </summary>
public sealed class SuperUILocalizer : ISuperUILocalizer
{
    private readonly LocalizationService _inner;

    /// <summary>
    /// Initializes with default options.
    /// </summary>
    public SuperUILocalizer() : this(null) { }

    /// <summary>
    /// Initializes with the supplied options.
    /// </summary>
    public SuperUILocalizer(IOptions<SuperUiOptions>? options)
    {
        _inner = new LocalizationService(options);
    }

    /// <inheritdoc/>
    public event Action? OnLocaleChanged
    {
        add => _inner.OnLocaleChanged += value;
        remove => _inner.OnLocaleChanged -= value;
    }

    /// <inheritdoc/>
    public string this[string key] => _inner[key];

    /// <inheritdoc/>
    public string GetString(string key, params object[] args) => _inner.GetString(key, args);

    /// <summary>
    /// Gets the current language code.
    /// </summary>
    public string CurrentLanguage => _inner.CurrentLanguage;

    /// <summary>
    /// Changes the current language and notifies subscribers.
    /// </summary>
    public void SetLanguage(string lang) => _inner.SetLanguage(lang);

    /// <inheritdoc/>
    public IEnumerable<string> SupportedLanguages => _inner.SupportedLanguages;
}
