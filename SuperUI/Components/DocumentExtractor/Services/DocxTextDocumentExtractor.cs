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
/// Library-free DOCX text + heuristic field extractor.
/// Reads <c>word/document.xml</c> from the .docx ZIP, pulls out paragraph text,
/// and detects "Label: value" / "Label = value" patterns as candidate fields.
///
/// This is the "extraction via other library" path the user asked for — it does not
/// require an LLM, runs offline, and produces a <see cref="SgDocumentExtractionResult"/>
/// that the saver can round-trip back into a DOCX.
/// </summary>
public sealed class DocxTextDocumentExtractor : IDocumentExtractor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public string Id => "docx-text";
    public string DisplayName => "DOCX text (built-in)";
    public IReadOnlyCollection<SgDocumentKind> SupportedKinds { get; } = new[] { SgDocumentKind.Docx };

    public bool CanHandle(SgDocumentSource source) => source.Kind == SgDocumentKind.Docx;

    /// <summary>Extracts plain text and heuristic label:value fields from a DOCX file.</summary>
    public Task<SgDocumentExtractionResult> ExtractAsync(SgDocumentSource source, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(source.Data, writable: false);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var docEntry = zip.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("Not a valid DOCX (missing word/document.xml).");

        using var stream = docEntry.Open();
        var xdoc = XDocument.Load(stream);

        var paragraphs = new List<string>();
        foreach (var p in xdoc.Descendants(W + "p"))
        {
            // Each <w:t> is a text run inside a paragraph; concatenate them in document order.
            var sb = new StringBuilder();
            foreach (var t in p.Descendants(W + "t"))
                sb.Append(t.Value);
            paragraphs.Add(sb.ToString());
        }

        var fields = DetectFields(paragraphs);
        var plain = string.Join('\n', paragraphs);

        return Task.FromResult(new SgDocumentExtractionResult
        {
            Source = source,
            Fields = fields,
            PlainText = plain,
            Metadata = { ["extractor"] = Id, ["paragraphCount"] = paragraphs.Count.ToString() }
        });
    }

    private static List<SgDocumentField> DetectFields(IReadOnlyList<string> paragraphs)
    {
        var result = new List<SgDocumentField>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var line = paragraphs[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Match "Label: value" or "Label = value"; label must be reasonably short.
            var sep = FindSeparator(line);
            if (sep <= 0 || sep > 60) continue;
            var label = line[..sep].Trim();
            var value = line[(sep + 1)..].Trim();
            if (label.Length == 0 || label.Length > 60) continue;
            if (!HasLetter(label)) continue;

            var key = ToKey(label);
            if (!seen.Add(key)) continue;

            result.Add(new SgDocumentField
            {
                Key = key,
                Label = label,
                Type = SgDocumentFieldType.Text,
                Value = value,
                Locator = $"paragraph:{i}"
            });
        }

        return result;
    }

    private static int FindSeparator(string line)
    {
        // Pick the first ':' or '=' not surrounded by digits (so we don't split times like 12:30).
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c != ':' && c != '=') continue;
            var prev = i > 0 ? line[i - 1] : ' ';
            var next = i + 1 < line.Length ? line[i + 1] : ' ';
            if (char.IsDigit(prev) && char.IsDigit(next)) continue;
            return i;
        }
        return -1;
    }

    private static bool HasLetter(string s) => s.Any(char.IsLetter);

    private static string ToKey(string label)
    {
        var sb = new StringBuilder(label.Length);
        var lastUnderscore = true;
        foreach (var c in label.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(c); lastUnderscore = false; }
            else if (!lastUnderscore) { sb.Append('_'); lastUnderscore = true; }
        }
        return sb.ToString().Trim('_');
    }
}
