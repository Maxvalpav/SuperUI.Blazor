using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

public sealed class PlainTextDocumentSaver : IDocumentSaver
{
    public string Id => "plaintext";
    public string DisplayName => "Plain text";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[] { SgDocumentKind.PlainText };

    public bool CanHandle(SgDocumentExtractionResult result) =>
        result.Source?.Kind == SgDocumentKind.PlainText;

    public Task<SgDocumentSource> SaveAsync(
        SgDocumentExtractionResult result,
        IReadOnlyList<SgDocumentField> editedFields,
        CancellationToken ct = default)
    {
        var src = result.Source!;
        var sb = new StringBuilder(result.PlainText ?? "");
        if (editedFields.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("---");
            foreach (var f in editedFields)
                sb.AppendLine($"{f.Label}: {f.Value}");
        }

        return Task.FromResult(new SgDocumentSource
        {
            FileName = AppendSuffix(src.FileName, "-edited"),
            MimeType = "text/plain",
            Kind = SgDocumentKind.PlainText,
            Data = Encoding.UTF8.GetBytes(sb.ToString())
        });
    }

    private static string AppendSuffix(string fileName, string suffix)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) ext = ".txt";
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return $"{stem}{suffix}{ext}";
    }
}
