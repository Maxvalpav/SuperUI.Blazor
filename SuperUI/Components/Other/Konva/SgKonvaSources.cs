namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Konva.js bundle.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgKonvaSources { KonvaScript = "/lib/konva/konva.min.js" }
/// </code>
/// </example>
public sealed class SgKonvaSources
{
    /// <summary>
    /// Konva.js UMD bundle (v9).
    /// Set to <c>null</c> if you load Konva yourself via index.html.
    /// </summary>
    public string? KonvaScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/konva@9.3.18/konva.min.js";
}
