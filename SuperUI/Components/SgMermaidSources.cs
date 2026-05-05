namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Mermaid.js bundle.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgMermaidSources { MermaidScript = "/lib/mermaid/mermaid.min.js" }
/// </code>
/// </example>
public sealed class SgMermaidSources
{
    /// <summary>
    /// Mermaid.js UMD bundle (v11).
    /// Set to <c>null</c> if you load Mermaid yourself via index.html.
    /// </summary>
    public string? MermaidScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.min.js";
}
