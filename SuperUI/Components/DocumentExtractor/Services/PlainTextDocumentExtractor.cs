using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Trivial extractor for plain-text uploads. Mostly useful as a fallback and so the
/// LLM extractor has something to chain with for .txt content.
/// </summary>
public sealed class PlainTextDocumentExtractor : IDocumentExtractor
{
    public string Id => "plaintext";
    public string DisplayName => "Plain text (built-in)";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[] { SgDocumentKind.PlainText };
    public bool CanHandle(SgDocumentSource source) => source.Kind == SgDocumentKind.PlainText;

    public Task<SgDocumentExtractionResult> ExtractAsync(SgDocumentSource source, CancellationToken ct = default)
    {
        var text = Encoding.UTF8.GetString(source.Data);
        return Task.FromResult(new SgDocumentExtractionResult
        {
            Source = source,
            PlainText = text,
            Fields = new List<SgDocumentField>(),
            Metadata = { ["extractor"] = Id }
        });
    }
}
