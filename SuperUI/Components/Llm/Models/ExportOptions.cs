using System;

namespace SuperUI.Components.Llm.Models;

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Pdf;
    public string FileName { get; set; } = $"document-extractor-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    public bool IncludeSourceMetadata { get; set; } = true;
    public bool IncludeSchemaSummary { get; set; } = true;
    public bool IncludeEmptyFields { get; set; }
    
    /// <summary>
    /// Использовать оригинальный файл как шаблон для экспорта (сохраняет структуру)
    /// </summary>
    public bool UseTemplateMode { get; set; } = true;
}

public class ExportedFile
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
