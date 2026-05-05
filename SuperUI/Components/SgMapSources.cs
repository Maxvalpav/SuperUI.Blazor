namespace SuperUI.Components;

/// <summary>
/// Source URLs for the OpenLayers bundle and optional CSS.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgMapSources
/// {
///     OlScript = "/lib/ol/ol.js",
///     OlCss    = "/lib/ol/ol.css"
/// }
/// </code>
/// </example>
public sealed class SgMapSources
{
    /// <summary>OpenLayers UMD bundle (v10).</summary>
    public string? OlScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/ol@10.3.1/dist/ol.js";

    /// <summary>OpenLayers default stylesheet.</summary>
    public string? OlCss { get; set; } =
        "https://cdn.jsdelivr.net/npm/ol@10.3.1/ol.css";
}
