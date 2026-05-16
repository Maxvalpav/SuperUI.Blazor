using System.Collections.Generic;

namespace SuperUI.Components.Llm.Models;

public enum FieldType
{
    Text, TextArea, Number, Integer,
    Date, DateTime, Boolean,
    Select, MultiSelect,
    Table, Image, Address, Phone, Email
}

public class FieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FieldType Type { get; set; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }
    public string? Group { get; set; }
    
    public List<SelectOption>? Options { get; set; }
    
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Format { get; set; }
    
    public List<FieldDefinition>? Columns { get; set; }
    
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SelectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
