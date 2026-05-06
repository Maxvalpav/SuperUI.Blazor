namespace SuperUI.Components;

public class SgMonacoOptions
{
    public string Language { get; set; } = "json";
    public string Theme { get; set; } = "vs"; // vs, vs-dark, hc-black
    public int FontSize { get; set; } = 13;
    public bool ReadOnly { get; set; } = false;
    public bool Minimap { get; set; } = false;
    public bool LineNumbers { get; set; } = true;
    public bool WordWrap { get; set; } = false;
    public bool AutoFormat { get; set; } = true;
}

public class SgMonacoSources
{
    public string LoaderScript { get; set; } = "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js";
    public string VsPath { get; set; } = "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs";
}

// JSON Schema models
public class SgJsonSchema
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "object";
    public Dictionary<string, SgJsonSchemaProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
    public Dictionary<string, SgJsonSchema>? Definitions { get; set; }
}

public class SgJsonSchemaProperty
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "string";
    public string? Format { get; set; } // date, date-time, email, uri, password, color
    public object? Default { get; set; }
    public List<object>? Enum { get; set; }
    public List<string>? EnumNames { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public bool ReadOnly { get; set; } = false;
    public bool WriteOnly { get; set; } = false;
    // For arrays
    public SgJsonSchemaProperty? Items { get; set; }
    // For objects
    public Dictionary<string, SgJsonSchemaProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
    // UI hints
    public string? UiWidget { get; set; } // textarea, password, color, slider, code, hidden
    public int? UiRows { get; set; }
    public string? UiGroup { get; set; }
    public int? UiOrder { get; set; }
}

public class SgJsonSchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
