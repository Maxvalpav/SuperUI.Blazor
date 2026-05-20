namespace SuperUI.Components;

/// <summary>
/// CDN source URLs for RAG vendor libraries.
/// Override any URL to use local copies or pin specific versions.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgRagSources
/// {
///     TransformersScript = "/lib/transformers/transformers.min.js",
///     PdfJsScript        = "/lib/pdfjs/pdf.min.js",
/// }
/// </code>
/// </example>
public sealed class SgRagSources
{
    /// <summary>
    /// @xenova/transformers ESM bundle for embedding models.
    /// Used for sentence-transformers inference in the browser.
    /// </summary>
    public string TransformersScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/@xenova/transformers@2.17.2/dist/transformers.min.js";

    /// <summary>
    /// @mlc-ai/web-llm ESM bundle for local LLM inference via WebGPU.
    /// Set to <c>null</c> if you only use OpenAI-compatible providers.
    /// </summary>
    public string? WebLlmScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm@0.2.83/lib/index.js";

    /// <summary>
    /// PDF.js main script for PDF text extraction.
    /// </summary>
    public string PdfJsScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.min.mjs";

    /// <summary>
    /// PDF.js worker script URL (must match the main script version).
    /// </summary>
    public string PdfJsWorker { get; set; } =
        "https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.worker.min.mjs";

    /// <summary>
    /// mammoth.js for DOCX text extraction.
    /// </summary>
    public string MammothScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/mammoth@1.8.0/mammoth.browser.min.js";

    /// <summary>
    /// marked.js for Markdown parsing.
    /// </summary>
    public string MarkedScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js";

    /// <summary>
    /// idb library for IndexedDB promise wrappers.
    /// </summary>
    public string IdbScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/idb@8.0.0/build/umd.js";
}
