namespace SuperUI.Components;

public class SgMonacoOptions
{
    /// <summary>Editor language (e.g. "json", "csharp", "xml"). Default "json".</summary>
    public string Language { get; set; } = "json";

    /// <summary>Editor theme: "vs", "vs-dark", "hc-black", "sg-auto", "sg-light", "sg-dark". Default "vs".</summary>
    public string Theme { get; set; } = "vs";

    /// <summary>Editor font size in px. Default 13.</summary>
    public int FontSize { get; set; } = 13;

    /// <summary>Whether the editor is read-only. Default false.</summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>Whether the minimap is visible. Default false.</summary>
    public bool Minimap { get; set; } = false;

    /// <summary>Whether line numbers are shown. Default true.</summary>
    public bool LineNumbers { get; set; } = true;

    /// <summary>Whether word wrap is enabled. Default false.</summary>
    public bool WordWrap { get; set; } = false;

    /// <summary>Auto-format JSON on load and external value changes. Default true.</summary>
    public bool AutoFormat { get; set; } = true;

    /// <summary>Custom monospace font family (CSS value). Default null (uses built-in stack).</summary>
    public string? FontFamily { get; set; }

    /// <summary>Whether font ligatures are enabled. Default null (true).</summary>
    public bool? FontLigatures { get; set; }

    /// <summary>Tab width in spaces. Default null (2).</summary>
    public int? TabSize { get; set; }

    /// <summary>Minimum editor height in px (native Monaco option). Default null.</summary>
    public int? MinHeight { get; set; }

    /// <summary>Maximum editor height in px (editor scrolls / grows to this). Default null.</summary>
    public int? MaxHeight { get; set; }

    // ── New optional properties ────────────────────────────────────────────────

    /// <summary>Whether to highlight occurrences of the selected word. Default true.</summary>
    public bool OccurrencesHighlight { get; set; } = true;

    /// <summary>Whether to highlight selections matching the selected word. Default true.</summary>
    public bool SelectionHighlight { get; set; } = true;

    /// <summary>Whether code folding is enabled. Default true.</summary>
    public bool Folding { get; set; } = true;

    /// <summary>Whether code lens (inline action hints) are shown. Default true.</summary>
    public bool CodeLens { get; set; } = true;

    /// <summary>Whether color decorators for color values are shown. Default true.</summary>
    public bool ColorDecorators { get; set; } = true;

    /// <summary>Whether clickable links in editor are enabled. Default true.</summary>
    public bool Links { get; set; } = true;

    /// <summary>Whether quick suggestions (intelliSense) are enabled. Default true.</summary>
    public bool QuickSuggestions { get; set; } = true;

    /// <summary>Whether parameter hints are shown while typing. Default true.</summary>
    public bool ParameterHints { get; set; } = true;

    /// <summary>Top padding in px inside the editor. Default 10.</summary>
    public int PaddingTop { get; set; } = 10;

    /// <summary>Bottom padding in px inside the editor. Default 10.</summary>
    public int PaddingBottom { get; set; } = 10;

    /// <summary>Whether to render whitespace characters. "none", "boundary", "selection", "trailing", "all". Default "selection".</summary>
    public string RenderWhitespace { get; set; } = "selection";

    /// <summary>Whether bracket pair colorization is enabled. Default true.</summary>
    public bool BracketPairColorization { get; set; } = true;

    /// <summary>Cursor style: "line", "block", "underline", "line-thin", "block-outline", "underline-thin". Default "line".</summary>
    public string CursorStyle { get; set; } = "line";

    /// <summary>Cursor blinking: "smooth", "blink", "phase", "expand", "solid". Default "smooth".</summary>
    public string CursorBlinking { get; set; } = "smooth";

    /// <summary>Whether sticky scroll (show current scope at top) is enabled. Default false.</summary>
    public bool StickyScroll { get; set; } = false;

    /// <summary>Whether format on paste is enabled. Default true.</summary>
    public bool FormatOnPaste { get; set; } = true;
}

public class SgMonacoSources
{
    public string LoaderScript { get; set; } = "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js";
    public string VsPath { get; set; } = "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs";
}

/// <summary>Editor cursor position.</summary>
public class SgMonacoCursorPosition
{
    public int LineNumber { get; set; } = 1;
    public int Column { get; set; } = 1;
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
    public string? Format { get; set; }
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
    public SgJsonSchemaProperty? Items { get; set; }
    public Dictionary<string, SgJsonSchemaProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
    public string? UiWidget { get; set; }
    public int? UiRows { get; set; }
    public string? UiGroup { get; set; }
    public int? UiOrder { get; set; }
}

public class SgJsonSchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
