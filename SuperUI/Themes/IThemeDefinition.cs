namespace SuperUI.Themes;

/// <summary>
/// Main theme definition interface for SuperUI.
/// </summary>
public interface IThemeDefinition
{
    /// <summary>Unique identifier for the theme.</summary>
    string Id { get; }

    /// <summary>Display name.</summary>
    string Name { get; }

    /// <summary>Theme description.</summary>
    string? Description { get; }

    /// <summary>Author name.</summary>
    string? Author { get; }

    /// <summary>Theme version.</summary>
    string Version { get; }

    /// <summary>Primitive tokens.</summary>
    IThemePrimitives Primitives { get; }

    /// <summary>Semantic tokens for light mode.</summary>
    IThemeSemantic Light { get; }

    /// <summary>Semantic tokens for dark mode. If null, dark mode is not supported.</summary>
    IThemeSemantic? Dark { get; }

    /// <summary>Component-specific overrides.</summary>
    IThemeComponents? Components { get; }

    /// <summary>Optional typography settings (heading scale, fonts).</summary>
    IThemeTypography? Typography { get; }

    /// <summary>Additional CSS for this theme.</summary>
    string? AdditionalCss { get; }

    /// <summary>Category for grouping in theme picker. Default "Core".</summary>
    string Category { get; }

    /// <summary>Generates complete CSS string for this theme.</summary>
    string GenerateCss();
}
