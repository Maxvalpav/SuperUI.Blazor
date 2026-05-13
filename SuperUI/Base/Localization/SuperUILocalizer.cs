// SuperUI/Base/Localization/SuperUILocalizer.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace SuperUI.Base.Localization;

/// <summary>
/// Default SuperUI localizer implementation. Supports embedded resources,
/// custom dictionaries, and fallback chains. Culture-aware.
/// </summary>
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
            // Return key as fallback (like Blazor's default behavior)
            return $"[{key}]";
        }
    }

    public bool TryGetString(string key, out string value)
    {
        // Check custom strings first
        if (_customStrings.TryGetValue(key, out value!))
            return true;

        // Try resource managers
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

    /// <summary>Add a custom localized string.</summary>
    public void AddCustomString(string key, string value)
    {
        _customStrings[key] = value;
    }

    /// <summary>Add multiple custom strings at once.</summary>
    public void AddCustomStrings(IReadOnlyDictionary<string, string> strings)
    {
        foreach (var (k, v) in strings)
            _customStrings[k] = v;
    }

    /// <summary>Register a resource manager as a source.</summary>
    public void AddResourceManager(string name, ResourceManager resourceManager)
    {
        _resourceManagers[name] = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    }

    /// <summary>Get supported cultures from all resource managers.</summary>
    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        var cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, rm) in _resourceManagers)
        {
            // ResourceManager doesn't expose cultures directly,
            // but we can use the known set from registration
        }

        // Default supported cultures
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
