namespace SuperUI.Components;

/// <summary>
/// Source URLs for the zxing-js library.
/// Override to ship local copies or pin a specific version.
/// </summary>
public sealed class SgBarcodeScannerSources
{
    /// <summary>
    /// zxing-js library URL (UMD bundle).
    /// </summary>
    public string? ZxingScript { get; set; } =
        "https://unpkg.com/@zxing/library@0.21.3/umd/index.min.js";
}
