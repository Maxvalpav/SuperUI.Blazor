using System;
using System.Collections.Generic;

namespace SuperUI.Components.DocumentExtractor.Models;

/// <summary>
/// Supported document kinds for upload/extraction/save.
/// </summary>
public enum SgDocumentKind
{
    Unknown = 0,
    Pdf,
    Docx,
    Image,
    PlainText
}

/// <summary>
/// Field types the form generator can render via SuperUI inputs.
/// </summary>
public enum SgDocumentFieldType
{
    Text,
    MultilineText,
    Number,
    Date,
    Boolean,
    Select,
    Image
}

/// <summary>
/// Raw file uploaded into the extractor pipeline.
/// </summary>
public sealed class SgDocumentSource
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public SgDocumentKind Kind { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// One extracted field exposed to the user as a form input.
/// </summary>
public sealed class SgDocumentField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public SgDocumentFieldType Type { get; set; } = SgDocumentFieldType.Text;
    public object? Value { get; set; }
    public List<string>? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? Hint { get; set; }
    public bool Required { get; set; }
    /// <summary>Locator inside the source document (e.g. page index, paragraph index, xpath). Used by the saver to round-trip changes.</summary>
    public string? Locator { get; set; }
}

/// <summary>
/// Result of running an <see cref="Services.IDocumentExtractor"/> against a source.
/// </summary>
public sealed class SgDocumentExtractionResult
{
    public List<SgDocumentField> Fields { get; set; } = new();
    /// <summary>Full plain-text view of the document, kept for re-saving (text round-trip) and for LLM context.</summary>
    public string? PlainText { get; set; }
    /// <summary>Original source — savers need this to round-trip the same format.</summary>
    public SgDocumentSource? Source { get; set; }
    /// <summary>Free-form metadata produced by the extractor (e.g. detected language, page count).</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
