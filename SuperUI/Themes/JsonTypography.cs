using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemeTypography"/>.</summary>
public sealed class JsonTypography : IThemeTypography
{
    [JsonPropertyName("googleFontsImportUrl")]   public string GoogleFontsImportUrl { get; set; } = "";
    [JsonPropertyName("embedGoogleFontsImport")] public bool EmbedGoogleFontsImport { get; set; } = true;
    [JsonPropertyName("headingFont")]            public string? HeadingFont { get; set; }
    [JsonPropertyName("serifFont")]              public string? SerifFont   { get; set; }
    [JsonPropertyName("displayFont")]            public string? DisplayFont { get; set; }
    [JsonPropertyName("medicalFont")]            public string? MedicalFont { get; set; }

    [JsonPropertyName("headings")]    public JsonHeadingScale        Headings    { get; set; } = new();
    [JsonPropertyName("phiScale")]    public JsonPhiTextScale?       PhiScale    { get; set; }
    [JsonPropertyName("phiLineHeight")] public JsonPhiLineHeightScale? PhiLineHeight { get; set; }

    HeadingSettings IThemeTypography.H1 => ToRecord(Headings.H1);
    HeadingSettings IThemeTypography.H2 => ToRecord(Headings.H2);
    HeadingSettings IThemeTypography.H3 => ToRecord(Headings.H3);
    HeadingSettings IThemeTypography.H4 => ToRecord(Headings.H4);
    HeadingSettings IThemeTypography.H5 => ToRecord(Headings.H5);
    HeadingSettings IThemeTypography.H6 => ToRecord(Headings.H6);

    PhiTextScale IThemeTypography.PhiScale => PhiScale is null
        ? new PhiTextScale()
        : new PhiTextScale(
            PhiScale.Micro   ?? "0.702rem",
            PhiScale.Caption ?? "0.875rem",
            PhiScale.Body    ?? "1rem",
            PhiScale.Lead    ?? "1.125rem",
            PhiScale.H3      ?? "1.618rem",
            PhiScale.H2      ?? "2.618rem",
            PhiScale.H1      ?? "4.236rem",
            PhiScale.Display ?? "6.854rem",
            PhiScale.Poster  ?? "11.09rem"
        );

    PhiLineHeightScale IThemeTypography.PhiLineHeight => PhiLineHeight is null
        ? new PhiLineHeightScale()
        : new PhiLineHeightScale(
            PhiLineHeight.Caption ?? "1.4",
            PhiLineHeight.Body    ?? "1.5",
            PhiLineHeight.Display ?? "1.1"
        );

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

public sealed class JsonPhiTextScale
{
    [JsonPropertyName("micro")]   public string? Micro   { get; set; }
    [JsonPropertyName("caption")] public string? Caption { get; set; }
    [JsonPropertyName("body")]    public string? Body    { get; set; }
    [JsonPropertyName("lead")]    public string? Lead    { get; set; }
    [JsonPropertyName("h3")]      public string? H3      { get; set; }
    [JsonPropertyName("h2")]      public string? H2      { get; set; }
    [JsonPropertyName("h1")]      public string? H1      { get; set; }
    [JsonPropertyName("display")] public string? Display { get; set; }
    [JsonPropertyName("poster")]  public string? Poster  { get; set; }
}

public sealed class JsonPhiLineHeightScale
{
    [JsonPropertyName("caption")] public string? Caption { get; set; }
    [JsonPropertyName("body")]    public string? Body    { get; set; }
    [JsonPropertyName("display")] public string? Display { get; set; }
}
