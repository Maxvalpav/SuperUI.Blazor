// ================================================================
// Файл: SuperUI/Base/Localization/SuperUILocalizer.cs
// ДОБАВЛЕНО: реализация GetString(string key, string defaultValue)
// ================================================================

using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace SuperUI.Base.Localization;

public class SuperUILocalizer : ISuperUILocalizer, IDisposable
{
    private readonly Dictionary<string, string> _customStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();
    private CultureInfo _culture;

    public event Action<CultureInfo>? CultureChanged;

    public SuperUILocalizer() : this(CultureInfo.CurrentUICulture) { }

    public SuperUILocalizer(CultureInfo culture)
    {
        _culture = culture ?? CultureInfo.CurrentUICulture;
    }

    public CultureInfo CurrentCulture
    {
        get => _culture;
        set
        {
            if (_culture.Equals(value)) return;
            _culture = value;
            CultureChanged?.Invoke(_culture);
        }
    }

    public string this[string key]
    {
        get
        {
            if (TryGetString(key, out var value))
                return value;
            return $"[{key}]";
        }
    }

    /// <summary>
    /// Get localized string with a default fallback value.
    /// </summary>
    public string GetString(string key, string defaultValue)
    {
        if (TryGetString(key, out var value))
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Get localized string with format parameters.
    /// </summary>
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

    public bool TryGetString(string key, out string value)
    {
        if (_customStrings.TryGetValue(key, out value!))
            return true;

        foreach (var (_, rm) in _resourceManagers)
        {
            value = rm.GetString(key, _culture);
            if (value != null)
                return true;
        }

        value = string.Empty;
        return false;
    }

    public string Format(string key, params object[] args)
    {
        var format = this[key];
        return string.Format(_culture, format, args);
    }

    public void AddCustomString(string key, string value)
        => _customStrings[key] = value;

    public void AddCustomStrings(IReadOnlyDictionary<string, string> strings)
    {
        foreach (var (k, v) in strings)
            _customStrings[k] = v;
    }

    public void AddResourceManager(string name, ResourceManager resourceManager)
        => _resourceManagers[name] = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        yield return new CultureInfo("en");
        yield return new CultureInfo("ru");
        yield return new CultureInfo("de");
        yield return new CultureInfo("fr");
        yield return new CultureInfo("es");
        yield return new CultureInfo("zh");
        yield return new CultureInfo("ja");
    }

    public void Dispose()
    {
        _customStrings.Clear();
        _resourceManagers.Clear();
        CultureChanged = null;
        GC.SuppressFinalize(this);
    }
}
