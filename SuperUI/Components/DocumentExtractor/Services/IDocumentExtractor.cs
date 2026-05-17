using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Pulls structured fields out of an uploaded document.
/// Implementations may use a library (PdfPig, OpenXml, OCR) or an LLM.
/// </summary>
public interface IDocumentExtractor
{
    /// <summary>Stable identifier shown in the UI extractor picker (e.g. "llm", "docx-openxml", "pdf-pdfpig").</summary>
    string Id { get; }

    /// <summary>Human-readable name for the picker.</summary>
    string DisplayName { get; }

    /// <summary>Document kinds this extractor can handle.</summary>
    IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; }

    bool CanHandle(SgDocumentSource source);

    Task<SgDocumentExtractionResult> ExtractAsync(SgDocumentSource source, CancellationToken ct = default);
}
