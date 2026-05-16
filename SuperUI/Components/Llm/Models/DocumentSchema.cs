using System;
using System.Collections.Generic;

namespace SuperUI.Components.Llm.Models;

public class DocumentSchema
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<FieldDefinition> Fields { get; set; } = new();
    public List<FormSection> Sections { get; set; } = new();
    
    public string? RawJsonSchema { get; set; }
    
    public Dictionary<string, string> DocumentMetadata { get; set; } = new();
}

public class FormSection
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> FieldKeys { get; set; } = new();
    public int Order { get; set; }
    public bool Collapsible { get; set; } = false;
}
