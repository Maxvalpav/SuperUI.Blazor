using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemeTypography"/>.</summary>
public sealed class JsonTypography : IThemeTypography
{
    [JsonPropertyName("googleFontsImportUrl")]   public string GoogleFontsImportUrl { get; set; } = "";
    [JsonPropertyName("embedGoogleFontsImport")] public bool EmbedGoogleFontsImport { get; set; } = true;
    [JsonPropertyName("headingFont")]            public string? HeadingFont { get; set; }

    [JsonPropertyName("headings")] public JsonHeadingScale Headings { get; set; } = new();

    HeadingSettings IThemeTypography.H1 => ToRecord(Headings.H1);
    HeadingSettings IThemeTypography.H2 => ToRecord(Headings.H2);
    HeadingSettings IThemeTypography.H3 => ToRecord(Headings.H3);
    HeadingSettings IThemeTypography.H4 => ToRecord(Headings.H4);
    HeadingSettings IThemeTypography.H5 => ToRecord(Headings.H5);
    HeadingSettings IThemeTypography.H6 => ToRecord(Headings.H6);

    private static HeadingSettings ToRecord(JsonHeadingSettings s) =>
        new(s.FontSize, s.FontFamily, s.FontWeight, s.LineHeight, s.LetterSpacing);
}

public sealed class JsonHeadingScale
{
    [JsonPropertyName("h1")] public JsonHeadingSettings H1 { get; set; } = new() { FontSize = "2.5rem", FontWeight = "700" };
    [JsonPropertyName("h2")] public JsonHeadingSettings H2 { get; set; } = new() { FontSize = "2rem",   FontWeight = "600" };
    [JsonPropertyName("h3")] public JsonHeadingSettings H3 { get; set; } = new() { FontSize = "1.5rem", FontWeight = "600" };
    [JsonPropertyName("h4")] public JsonHeadingSettings H4 { get; set; } = new() { FontSize = "1.25rem",FontWeight = "600" };
    [JsonPropertyName("h5")] public JsonHeadingSettings H5 { get; set; } = new() { FontSize = "1.125rem",FontWeight = "600" };
    [JsonPropertyName("h6")] public JsonHeadingSettings H6 { get; set; } = new() { FontSize = "1rem",   FontWeight = "500" };
}

public sealed class JsonHeadingSettings
{
    [JsonPropertyName("fontSize")]      public string FontSize      { get; set; } = "";
    [JsonPropertyName("fontFamily")]    public string? FontFamily   { get; set; }
    [JsonPropertyName("fontWeight")]    public string? FontWeight   { get; set; }
    [JsonPropertyName("lineHeight")]    public string? LineHeight   { get; set; }
    [JsonPropertyName("letterSpacing")] public string? LetterSpacing { get; set; }
}
