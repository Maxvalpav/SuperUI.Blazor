using System.Threading.Tasks;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class ParsedDocument
{
    public string ExtractedText { get; set; } = string.Empty;
    public List<PageImage> Pages { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PageImage
{
    public int PageNumber { get; set; }
    public string Base64 { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/png";
}

public interface IDocumentParserService
{
    Task<ParsedDocument> ParseAsync(UploadedFile file);
}
