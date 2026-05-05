namespace SuperUI.Components;

/// <summary>
/// Source URLs for the D3.js bundle and optional extension scripts.
/// Override to ship local copies or pin specific versions.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgD3Sources { D3Script = "/lib/d3/d3.min.js" }
/// </code>
/// </example>
public sealed class SgD3Sources
{
    /// <summary>
    /// D3.js UMD bundle (v7).
    /// Set to <c>null</c> if you load D3 yourself (e.g. via index.html).
    /// </summary>
    public string? D3Script { get; set; } =
        "https://cdn.jsdelivr.net/npm/d3@7.9.0/dist/d3.min.js";
}
