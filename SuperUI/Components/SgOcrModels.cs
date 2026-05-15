using SuperUI.Enums;

namespace SuperUI.Components;

// ── Language ──────────────────────────────────────────────────────────────────
// SgOcrLanguage — moved to SuperUI.Enums.SgOcrLanguage

// ── Result ────────────────────────────────────────────────────────────────────

/// <summary>OCR recognition result returned by <see cref="SgOcr"/>.</summary>
public class SgOcrResult
{
    /// <summary>Full extracted text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Overall confidence score (0–100).</summary>
    public double Confidence { get; set; }

    /// <summary>Recognised words with individual confidence scores.</summary>
    public List<SgOcrWord> Words { get; set; } = new();

    /// <summary>Recognised text lines.</summary>
    public List<SgOcrLine> Lines { get; set; } = new();

    /// <summary>Language used for recognition.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Processing time in milliseconds.</summary>
    public long DurationMs { get; set; }
}

/// <summary>A single recognised word.</summary>
public class SgOcrWord
{
    public string Text       { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public SgOcrBbox Bbox    { get; set; } = new();
}

/// <summary>A single recognised text line.</summary>
public class SgOcrLine
{
    public string Text       { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public SgOcrBbox Bbox    { get; set; } = new();
}

/// <summary>Bounding box of a recognised element in pixels.</summary>
public class SgOcrBbox
{
    public int X0 { get; set; }
    public int Y0 { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
}

// ── Progress ──────────────────────────────────────────────────────────────────

/// <summary>Progress event fired during OCR processing.</summary>
public class SgOcrProgress
{
    /// <summary>Current processing status message.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Progress value 0–100, or -1 if indeterminate.</summary>
    public int Progress { get; set; } = -1;
}
