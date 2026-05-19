using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace SuperUI.Localization;

/// <summary>
/// Default implementation of <see cref="ISuperUILocalizer"/> using .resx resource files.
/// </summary>
public sealed class SuperUILocalizer : ISuperUILocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly CultureInfo? _fixedCulture;

    /// <summary>
    /// Initializes a new instance of <see cref="SuperUILocalizer"/>. Reads
    /// <see cref="CultureInfo.CurrentUICulture"/> on each lookup so language switches
    /// take effect immediately without re-creating the service.
    /// </summary>
    public SuperUILocalizer()
    {
        _fixedCulture = new CultureInfo("ru-RU");
        _resourceManager = new ResourceManager(
            "SuperUI.Resources.SuperUIStrings",
            typeof(SuperUILocalizer).Assembly);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SuperUILocalizer"/> pinned to a specific culture.
    /// </summary>
    /// <param name="culture">The culture to use for localization.</param>
    public SuperUILocalizer(CultureInfo culture)
    {
        _fixedCulture = culture;
        _resourceManager = new ResourceManager(
            "SuperUI.Resources.SuperUIStrings",
            typeof(SuperUILocalizer).Assembly);
    }

    private CultureInfo Culture => _fixedCulture ?? CultureInfo.CurrentUICulture;

    /// <inheritdoc/>
    public string this[string key]
    {
        get
        {
            try
            {
                return _resourceManager.GetString(key, Culture) ?? key;
            }
            catch (Exception ex)
            {
                Debug.Fail($"[SuperUI] Localization lookup failed for key '{key}': {ex.Message}");
                return key;
            }
        }
    }

    /// <inheritdoc/>
    public string GetString(string key, params object[] args)
    {
        var format = this[key];
        try
        {
            return string.Format(Culture, format, args);
        }
        catch (Exception ex)
        {
            Debug.Fail($"[SuperUI] string.Format failed for key '{key}': {ex.Message}");
            return format;
        }
    }
}
