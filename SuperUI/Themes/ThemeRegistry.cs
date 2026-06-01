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
    [Obsolete("Use RegisterJson(string path) and ship a JSON file under Themes/json/. Will be removed in 3.0.")]
    public void Register(IThemeDefinition theme)
    {
        _themes[theme.Id] = theme;
    }

    /// <summary>Registers a theme from a JSON file on disk or embedded resource.</summary>
    /// <param name="id">Theme identifier (also used as JSON <c>id</c> override).</param>
    /// <param name="json">Raw JSON text.</param>
    public void RegisterJson(string id, string json)
    {
        var theme = JsonThemeDefinition.FromJson(json);
        _themes[theme.Id] = theme;
    }

    /// <summary>Registers a theme from a JSON file on disk.</summary>
    public void RegisterJsonFromFile(string path)
    {
        var json = File.ReadAllText(path);
        RegisterJson(Path.GetFileNameWithoutExtension(path), json);
    }

    /// <summary>
    /// Loads all JSON themes embedded under <c>SuperUI.Themes.json.*</c> (see
    /// <c>&lt;EmbeddedResource Include="Themes\json\*.json" /&gt;</c> in <c>SuperUI.csproj</c>).
    /// Replaces any themes registered with the same id.
    /// </summary>
    /// <returns>Number of themes loaded.</returns>
    public int LoadEmbeddedJsonThemes()
    {
        var assembly = typeof(ThemeRegistry).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("SuperUI.Themes.json.", StringComparison.Ordinal)
                        && r.EndsWith(".json", StringComparison.Ordinal));

        var loaded = 0;
        foreach (var resource in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            try
            {
                var theme = JsonThemeDefinition.FromJson(json);
                _themes[theme.Id] = theme;
                loaded++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load embedded theme '{resource}': {ex.Message}", ex);
            }
        }
        return loaded;
    }

    /// <summary>Async wrapper over <see cref="LoadEmbeddedJsonThemes"/>.</summary>
    public Task<int> LoadEmbeddedJsonThemesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LoadEmbeddedJsonThemes());
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
