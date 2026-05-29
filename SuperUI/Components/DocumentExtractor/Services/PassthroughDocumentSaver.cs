using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Fallback saver for formats whose bytes we cannot rewrite without a heavy native library
/// (PDF, raster images). Returns the original file unchanged so the user gets back the same
/// kind of document they uploaded. The edited field values are embedded as JSON metadata in
/// a companion file via <see cref="BuildSidecar"/>, so callers can persist edits alongside.
/// Swap this implementation out for a library-backed one (PdfPig, ImageSharp) to do true round-trips.
/// </summary>
public sealed class PassthroughDocumentSaver : IDocumentSaver
{
    public string Id => "passthrough";
    public string DisplayName => "Original file (passthrough)";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[] { SgDocumentKind.Pdf, SgDocumentKind.Image };

    public bool CanHandle(SgDocumentExtractionResult result) =>
        result.Source is { } s && (s.Kind == SgDocumentKind.Pdf || s.Kind == SgDocumentKind.Image);

    /// <summary>Returns the original document unchanged (passthrough — no native library available for true round-trip).</summary>
    public Task<SgDocumentSource> SaveAsync(
        SgDocumentExtractionResult result,
        IReadOnlyList<SgDocumentField> editedFields,
        CancellationToken ct = default)
    {
        var src = result.Source!;
        return Task.FromResult(new SgDocumentSource
        {
            FileName = src.FileName,
            MimeType = src.MimeType,
            Kind = src.Kind,
            Data = src.Data
        });
    }

    /// <summary>Builds a JSON sidecar describing edits — callers may download/save it next to the original.</summary>
    public static SgDocumentSource BuildSidecar(SgDocumentSource original, IReadOnlyList<SgDocumentField> editedFields)
    {
        var payload = new
        {
            sourceFile = original.FileName,
            fields = editedFields
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var stem = Path.GetFileNameWithoutExtension(original.FileName);
        return new SgDocumentSource
        {
            FileName = $"{stem}.fields.json",
            MimeType = "application/json",
            Kind = SgDocumentKind.PlainText,
            Data = Encoding.UTF8.GetBytes(json)
        };
    }
}
