using System.Collections.Generic;

namespace SuperUI.Components;

/// <summary>Metadata describing the structure of a dynamically generated smart form.</summary>
public class SgSmartFormMetadata
{
    /// <summary>Gets or sets the form title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the form description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the list of field metadata definitions.</summary>
    public List<SgSmartFieldMetadata> Fields { get; set; } = new();
    /// <summary>Gets or sets the number of grid columns for field layout.</summary>
    public int Columns { get; set; } = 1;
}

/// <summary>Metadata describing a single field in a smart form.</summary>
public class SgSmartFieldMetadata
{
    /// <summary>Gets or sets the unique key identifier for the field.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label for the field.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional description text for the field.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the placeholder text for the field.</summary>
    public string? Placeholder { get; set; }
    /// <summary>Gets or sets the field type (text, number, boolean, etc.).</summary>
    public SgSmartFieldType Type { get; set; } = SgSmartFieldType.Text;
    /// <summary>Gets or sets whether the field is required.</summary>
    public bool Required { get; set; }
    /// <summary>Gets or sets whether the field is read-only.</summary>
    public bool ReadOnly { get; set; }
    /// <summary>Gets or sets whether the field spans the full form width.</summary>
    public bool FullWidth { get; set; }
    /// <summary>Gets or sets the group name for organizing fields into sections.</summary>
    public string? Group { get; set; }
    /// <summary>Gets or sets the options for select/enum field types.</summary>
    public List<SgSmartOption>? Options { get; set; }
    /// <summary>Gets or sets the default value for the field.</summary>
    public object? DefaultValue { get; set; }
    /// <summary>Gets or sets optional validation rules for the field.</summary>
    public Dictionary<string, object>? ValidationRules { get; set; }
}

/// <summary>Defines the supported field types for a smart form.</summary>
public enum SgSmartFieldType
{
    /// <summary>Single-line text input.</summary>
    Text,
    /// <summary>Multi-line text area.</summary>
    MultilineText,
    /// <summary>Numeric input.</summary>
    Number,
    /// <summary>Boolean toggle / checkbox.</summary>
    Boolean,
    /// <summary>Date picker (date only).</summary>
    Date,
    /// <summary>Date and time picker.</summary>
    DateTime,
    /// <summary>Dropdown select from options.</summary>
    Select,
    /// <summary>Password input (masked).</summary>
    Password,
    /// <summary>Email input with validation.</summary>
    Email
}

/// <summary>Represents an option item for select/enum fields in a smart form.</summary>
public class SgSmartOption
{
    /// <summary>Gets or sets the display label for the option.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the underlying value for the option.</summary>
    public object Value { get; set; } = default!;
}
