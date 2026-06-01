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
        Register(new RoyalTheme());
        Register(new GraphiteTheme());
        Register(new ForestTheme());
        Register(new NeonTheme());
        Register(new GlassTheme());
        Register(new SignatureTheme());
        Register(new ChronoTheme());
        Register(new InclusTheme());
        Register(new SylvanTheme());
        Register(new ReaderTheme());
        Register(new WaveTheme());
        Register(new AureaTheme());
        Register(new CantusTheme());
        Register(new FractalisTheme());
        Register(new CosmosTheme());
        Register(new GordianTheme());
        // ═══ Flagship ✦ — новые 14 тем ═══
        Register(new CalyxTheme());
        Register(new ApexTheme());
        Register(new MediciTheme());
        Register(new ZenTheme());
        Register(new AetherTheme());
        Register(new OasisTheme());
        Register(new NeoTheme());
        Register(new ClarityTheme());
        Register(new ElementTheme());
        Register(new RadiusTheme());
        Register(new FluxTheme());
        Register(new MuseTheme());
        Register(new ForgeTheme());
        Register(new PrismTheme());
        // ═══ Science & Accessibility themes ═══
        Register(new ClarityClinicalTheme());
        Register(new CircadianTheme());
        Register(new ErgoTheme());
        Register(new BiofiliaTheme());
        Register(new LuminaTheme());
        // ═══ Glassmorphism themes ═══
        Register(new GlassLightTheme());
        Register(new GlassDarkTheme());
        Register(new GlassTintedTheme());
        Register(new GlassNeumorphicTheme());
        Register(new VeiledTheme());
        Register(new WindowTheme());
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
