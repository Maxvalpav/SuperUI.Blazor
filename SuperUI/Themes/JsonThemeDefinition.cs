using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>
/// JSON-driven theme definition. Mirrors <see cref="IThemeDefinition"/> so
/// <see cref="SgThemeGenerator"/> can consume it without changes.
/// Load via <c>JsonThemeDefinition.FromJson(...)</c> or <c>ThemeRegistry.RegisterJson(path)</c>.
/// </summary>
public sealed class JsonThemeDefinition : IThemeDefinition
{
    [JsonPropertyName("id")]              public string Id { get; set; } = "";
    [JsonPropertyName("name")]            public string Name { get; set; } = "";
    [JsonPropertyName("description")]     public string? Description { get; set; }
    [JsonPropertyName("author")]          public string? Author { get; set; }
    [JsonPropertyName("version")]         public string Version { get; set; } = "1.0.0";
    [JsonPropertyName("category")]        public string Category { get; set; } = "Core";
    [JsonPropertyName("additionalCss")]   public string? AdditionalCss { get; set; }

    [JsonPropertyName("primitives")]      public JsonPrimitives Primitives { get; set; } = new();
    [JsonPropertyName("light")]           public JsonSemantic Light { get; set; } = new();
    [JsonPropertyName("dark")]            public JsonSemantic? Dark { get; set; }
    [JsonPropertyName("components")]      public JsonComponents? Components { get; set; }
    [JsonPropertyName("typography")]      public JsonTypography? Typography { get; set; }

    IThemePrimitives IThemeDefinition.Primitives => Primitives;
    IThemeSemantic  IThemeDefinition.Light      => Light;
    IThemeSemantic? IThemeDefinition.Dark       => Dark;
    IThemeComponents? IThemeDefinition.Components => Components;
    IThemeTypography? IThemeDefinition.Typography => Typography;

    public string GenerateCss() => SgThemeGenerator.GenerateFullThemeCss(this);

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static JsonThemeDefinition FromJson(string json) =>
        JsonSerializer.Deserialize<JsonThemeDefinition>(json, _options)
            ?? throw new InvalidOperationException("Failed to parse theme JSON.");

    public static JsonThemeDefinition FromStream(Stream stream) =>
        JsonSerializer.Deserialize<JsonThemeDefinition>(stream, _options)
            ?? throw new InvalidOperationException("Failed to parse theme JSON stream.");
}
