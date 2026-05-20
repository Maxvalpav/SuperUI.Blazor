using System.Collections.Generic;

namespace SuperUI.Components;

public class SgSmartFormMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<SgSmartFieldMetadata> Fields { get; set; } = new();
    public int Columns { get; set; } = 1;
}

public class SgSmartFieldMetadata
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Placeholder { get; set; }
    public SgSmartFieldType Type { get; set; } = SgSmartFieldType.Text;
    public bool Required { get; set; }
    public bool ReadOnly { get; set; }
    public bool FullWidth { get; set; }
    public string? Group { get; set; }
    public List<SgSmartOption>? Options { get; set; } // For Select/Radio/Enum
    public object? DefaultValue { get; set; }
    public Dictionary<string, object>? ValidationRules { get; set; }
}

public enum SgSmartFieldType
{
    Text,
    MultilineText,
    Number,
    Boolean,
    Date,
    DateTime,
    Select,
    Password,
    Email
}

public class SgSmartOption
{
    public string Label { get; set; } = string.Empty;
    public object Value { get; set; } = default!;
}
