using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Writes edited form fields back into a document of the same kind as the original.
/// One implementation per output format keeps the pipeline modular.
/// </summary>
public interface IDocumentSaver
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; }

    bool CanHandle(SgDocumentExtractionResult result);

    /// <summary>Produces the saved file bytes (same kind/MIME as the original source).</summary>
    Task<SgDocumentSource> SaveAsync(
        SgDocumentExtractionResult result,
        IReadOnlyList<SgDocumentField> editedFields,
        CancellationToken ct = default);
}
