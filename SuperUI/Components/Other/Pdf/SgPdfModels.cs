using System;
using System.Collections.Generic;

namespace SuperUI.Components.Other.Pdf;

public enum SgPdfAnnotationType
{
    Text,
    Drawing,
    Highlight
}

public class SgPdfAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int PageNumber { get; set; }
    public SgPdfAnnotationType Type { get; set; }
    public string? Content { get; set; } // For text
    public string? FabricJson { get; set; } // For Fabric.js objects
    public string Color { get; set; } = "#ff0000";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class SgPdfFormField
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // Text, Checkbox, etc.
    public string? Value { get; set; }
    public bool IsReadOnly { get; set; }
}
