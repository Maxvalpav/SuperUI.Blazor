using System.Globalization;
using System.Resources;

namespace SuperUI.Localization;

/// <summary>
/// Default implementation of <see cref="ISuperUILocalizer"/> using .resx resource files.
/// </summary>
public sealed class SuperUILocalizer : ISuperUILocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of <see cref="SuperUILocalizer"/>.
    /// </summary>
    public SuperUILocalizer() : this(CultureInfo.CurrentUICulture)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SuperUILocalizer"/> with a specific culture.
    /// </summary>
    /// <param name="culture">The culture to use for localization.</param>
    public SuperUILocalizer(CultureInfo culture)
    {
        _culture = culture;
        _resourceManager = new ResourceManager(
            "SuperUI.Resources.SuperUIStrings",
            typeof(SuperUILocalizer).Assembly);
    }

    /// <inheritdoc/>
    public string this[string key]
    {
        get
        {
            try
            {
                var value = _resourceManager.GetString(key, _culture);
                return value ?? key;
            }
            catch
            {
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
            return string.Format(_culture, format, args);
        }
        catch
        {
            return format;
        }
    }
}
