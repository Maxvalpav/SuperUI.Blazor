using System.Collections.Concurrent;

namespace SuperUI.Themes;

/// <summary>
/// Registry for all available SuperUI themes.
/// </summary>
public sealed class ThemeRegistry
{
    private readonly ConcurrentDictionary<string, IThemeDefinition> _themes = new();
    private string _defaultThemeId = "natura-ui";

    public ThemeRegistry()
    {
        // Register built-in themes
        Register(new NaturaTheme());
        Register(new SolarisTheme());
    }

    /// <summary>Registers a new theme.</summary>
    public void Register(IThemeDefinition theme)
    {
        _themes[theme.Id] = theme;
    }

    /// <summary>Removes a theme by ID.</summary>
    public bool Unregister(string id)
    {
        if (id == _defaultThemeId) return false;
        return _themes.TryRemove(id, out _);
    }

    /// <summary>Gets a theme by ID.</summary>
    public bool TryGet(string id, out IThemeDefinition? theme)
    {
        return _themes.TryGetValue(id, out theme);
    }

    /// <summary>Gets the default theme.</summary>
    public IThemeDefinition GetDefault()
    {
        return _themes[_defaultThemeId];
    }

    /// <summary>Gets all registered themes.</summary>
    public IReadOnlyList<IThemeDefinition> GetAll()
    {
        return _themes.Values.ToList();
    }

    /// <summary>Sets the default theme ID.</summary>
    public void SetDefault(string id)
    {
        if (_themes.ContainsKey(id))
        {
            _defaultThemeId = id;
        }
    }
}
