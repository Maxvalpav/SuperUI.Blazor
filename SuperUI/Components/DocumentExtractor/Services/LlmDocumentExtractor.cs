using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Extractor that delegates to an LLM via <see cref="ILlmExtractionClient"/>.
/// Pre-extracts plain text from DOCX (and accepts pre-extracted text from a co-extractor)
/// so the model has something to anchor to even for non-vision providers.
/// </summary>
public sealed class LlmDocumentExtractor : IDocumentExtractor
{
    private readonly ILlmExtractionClient _client;
    private readonly Func<SgLlmEndpointConfig> _configProvider;
    private readonly IDocumentExtractor? _textExtractor;

    /// <summary>The text extractor (if any) used to pre-parse DOCX/PlainText before sending to the LLM.</summary>
    public LlmDocumentExtractor(
        ILlmExtractionClient client,
        Func<SgLlmEndpointConfig> configProvider,
        IDocumentExtractor? textExtractor = null)
    {
        _client = client;
        _configProvider = configProvider;
        _textExtractor = textExtractor;
    }

    public string Id => "llm";
    public string DisplayName => "LLM (OpenAI / OpenRouter compatible)";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[]
    {
        SgDocumentKind.Pdf, SgDocumentKind.Docx, SgDocumentKind.Image, SgDocumentKind.PlainText
    };

    public bool CanHandle(SgDocumentSource source) => source.Kind != SgDocumentKind.Unknown;

    /// <summary>Extracts document fields by sending the document content to a configured LLM.</summary>
    public async Task<SgDocumentExtractionResult> ExtractAsync(SgDocumentSource source, CancellationToken ct = default)
    {
        string? plainText = null;
        if (_textExtractor != null && _textExtractor.CanHandle(source))
        {
            try
            {
                var pre = await _textExtractor.ExtractAsync(source, ct).ConfigureAwait(false);
                plainText = pre.PlainText;
            }
            catch
            {
                // Pre-extraction is best-effort; the LLM still works from the image / raw context.
            }
        }

        var fields = await _client.ExtractFieldsAsync(_configProvider(), source, plainText, ct).ConfigureAwait(false);

        return new SgDocumentExtractionResult
        {
            Fields = fields,
            PlainText = plainText,
            Source = source,
            Metadata = { ["extractor"] = Id }
        };
    }
}
