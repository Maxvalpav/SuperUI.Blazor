using System;
using System.Collections.Generic;

namespace SuperUI.Components.Llm.Models;

public class ExtractedData
{
    public string SchemaId { get; set; } = string.Empty;
    public DocumentSchema Schema { get; set; } = new();
    
    public Dictionary<string, object?> Values { get; set; } = new();
    
    public List<UploadedFile> SourceFiles { get; set; } = new();
    
    public ExportTemplate? Template { get; set; }
    
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public bool IsModified { get; set; } = false;
}

public class UploadedFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
    public FileCategory Category { get; set; }
    
    public string Base64Content => Convert.ToBase64String(Content);
}

public enum FileCategory { Pdf, Word, Image, Unknown }

public class ExportTemplate
{
    public ExportFormat Format { get; set; }
    public string? TemplateFileId { get; set; }
    public Dictionary<string, string> StyleMap { get; set; } = new();
}

public enum ExportFormat { Pdf, Word, Both }
