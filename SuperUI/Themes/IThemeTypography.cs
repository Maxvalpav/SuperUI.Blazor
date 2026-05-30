namespace SuperUI.Themes;

/// <summary>Heading font settings for a single heading level (h1-h6).</summary>
/// <param name="FontSize">CSS font-size value.</param>
/// <param name="FontFamily">Optional font-family override for this heading level.</param>
/// <param name="FontWeight">CSS font-weight value.</param>
/// <param name="LineHeight">CSS line-height value.</param>
/// <param name="LetterSpacing">CSS letter-spacing value.</param>
public sealed record HeadingSettings(
    string FontSize,
    string? FontFamily = null,
    string? FontWeight = null,
    string? LineHeight = null,
    string? LetterSpacing = null
);

/// <summary>
/// Optional typography settings for a theme. When null, default values from
/// <see cref="IThemeSemantic"/> and browser defaults are used.
/// </summary>
public interface IThemeTypography
{
    /// <summary>Google Fonts @import URL to load the theme's font families.</summary>
    string GoogleFontsImportUrl { get; }

    /// <summary>Whether to embed the Google Fonts @import in generated CSS. Set to false when fonts are loaded externally.</summary>
    bool EmbedGoogleFontsImport => true;

    /// <summary>Optional font-family override for all heading levels.</summary>
    string? HeadingFont { get; }

    /// <summary>Settings for h1.</summary>
    HeadingSettings H1 { get; }

    /// <summary>Settings for h2.</summary>
    HeadingSettings H2 { get; }

    /// <summary>Settings for h3.</summary>
    HeadingSettings H3 { get; }

    /// <summary>Settings for h4.</summary>
    HeadingSettings H4 { get; }

    /// <summary>Settings for h5.</summary>
    HeadingSettings H5 { get; }

    /// <summary>Settings for h6.</summary>
    HeadingSettings H6 { get; }
}
