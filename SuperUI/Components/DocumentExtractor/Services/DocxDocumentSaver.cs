using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Saves edited fields back into the original DOCX.
/// For fields with locator <c>paragraph:{index}</c> we rewrite the matching paragraph
/// as "<c>Label: NewValue</c>" preserving the surrounding document. Other fields are
/// appended as new paragraphs so nothing is silently dropped.
/// </summary>
public sealed class DocxDocumentSaver : IDocumentSaver
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public string Id => "docx";
    public string DisplayName => "DOCX";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[] { SgDocumentKind.Docx };

    public bool CanHandle(SgDocumentExtractionResult result) =>
        result.Source?.Kind == SgDocumentKind.Docx && result.Source.Data.Length > 0;

    /// <summary>Saves edited fields back into a copy of the original DOCX document.</summary>
    public Task<SgDocumentSource> SaveAsync(
        SgDocumentExtractionResult result,
        IReadOnlyList<SgDocumentField> editedFields,
        CancellationToken ct = default)
    {
        var src = result.Source ?? throw new InvalidOperationException("Source DOCX missing.");

        // Work on a writable copy of the original ZIP.
        using var ms = new MemoryStream();
        ms.Write(src.Data, 0, src.Data.Length);
        ms.Position = 0;

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var docEntry = zip.GetEntry("word/document.xml")
                ?? throw new InvalidDataException("Not a valid DOCX (missing word/document.xml).");

            XDocument xdoc;
            using (var read = docEntry.Open())
                xdoc = XDocument.Load(read);

            var paragraphs = xdoc.Descendants(W + "p").ToList();
            var byLocator = editedFields
                .Where(f => !string.IsNullOrEmpty(f.Locator) && f.Locator!.StartsWith("paragraph:", StringComparison.Ordinal))
                .ToDictionary(f => f.Locator!, f => f);

            foreach (var (locator, field) in byLocator)
            {
                if (!TryParseIndex(locator, out var idx) || idx < 0 || idx >= paragraphs.Count) continue;
                ReplaceParagraphText(paragraphs[idx], $"{field.Label}: {field.Value}");
            }

            // Anything without a known paragraph anchor gets appended so edits aren't lost.
            var body = xdoc.Descendants(W + "body").FirstOrDefault();
            if (body != null)
            {
                foreach (var f in editedFields)
                {
                    if (!string.IsNullOrEmpty(f.Locator) && byLocator.ContainsKey(f.Locator!)) continue;
                    body.Add(BuildParagraph($"{f.Label}: {f.Value}"));
                }
            }

            docEntry.Delete();
            var fresh = zip.CreateEntry("word/document.xml", CompressionLevel.Optimal);
            using var write = fresh.Open();
            xdoc.Save(write);
        }

        return Task.FromResult(new SgDocumentSource
        {
            FileName = AppendSuffix(src.FileName, "-edited"),
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Kind = SgDocumentKind.Docx,
            Data = ms.ToArray()
        });
    }

    private static bool TryParseIndex(string locator, out int idx)
    {
        idx = -1;
        var colon = locator.IndexOf(':');
        return colon > 0 && int.TryParse(locator.AsSpan(colon + 1), out idx);
    }

    private static void ReplaceParagraphText(XElement paragraph, string newText)
    {
        // Drop all runs and put a single fresh run with the new text. Formatting from the
        // first run (if any) is preserved by reusing its <w:rPr>.
        var firstRun = paragraph.Descendants(W + "r").FirstOrDefault();
        var rPr = firstRun?.Element(W + "rPr");

        foreach (var r in paragraph.Descendants(W + "r").ToList())
            r.Remove();

        var newRun = new XElement(W + "r");
        if (rPr != null) newRun.Add(new XElement(rPr));
        newRun.Add(new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), newText));
        paragraph.Add(newRun);
    }

    private static XElement BuildParagraph(string text) =>
        new(W + "p",
            new XElement(W + "r",
                new XElement(W + "t",
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    text)));

    private static string AppendSuffix(string fileName, string suffix)
    {
        var ext = Path.GetExtension(fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return $"{stem}{suffix}{ext}";
    }
}
