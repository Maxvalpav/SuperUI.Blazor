namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Tesseract.js bundle and worker scripts.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <example>
/// Use local files (e.g. after <c>npm install tesseract.js</c>):
/// <code>
/// new SgOcrSources
/// {
///     TesseractScript = "/lib/tesseract/tesseract.min.js",
///     WorkerPath      = "/lib/tesseract/worker.min.js",
///     CorePath        = "/lib/tesseract/tesseract-core.wasm.js",
///     LangPath        = "/lib/tesseract/lang-data/"
/// }
/// </code>
/// </example>
public sealed class SgOcrSources
{
    /// <summary>
    /// Tesseract.js UMD bundle (v5).
    /// Set to <c>null</c> if you load Tesseract.js yourself via index.html.
    /// </summary>
    public string? TesseractScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/tesseract.js@5.1.1/dist/tesseract.min.js";

    /// <summary>
    /// URL of the Tesseract worker script.
    /// Defaults to the jsDelivr CDN path that matches the bundle version.
    /// </summary>
    public string WorkerPath { get; set; } =
        "https://cdn.jsdelivr.net/npm/tesseract.js@5.1.1/dist/worker.min.js";

    /// <summary>
    /// URL of the Tesseract WASM core.
    /// </summary>
    public string CorePath { get; set; } =
        "https://cdn.jsdelivr.net/npm/tesseract.js-core@5.1.1/tesseract-core.wasm.js";

    /// <summary>
    /// Base URL for language training data (.traineddata files).
    /// </summary>
    public string LangPath { get; set; } =
        "https://tessdata.projectnaptha.com/4.0.0/";
}
