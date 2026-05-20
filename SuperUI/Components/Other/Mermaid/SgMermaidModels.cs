using SuperUI.Enums;

namespace SuperUI.Components;

// ── Diagram type ──────────────────────────────────────────────────────────────
// SgMermaidDiagramType — moved to SuperUI.Enums.SgMermaidDiagramType

// ── Theme ─────────────────────────────────────────────────────────────────────
// SgMermaidTheme — moved to SuperUI.Enums.SgMermaidTheme

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgMermaid"/>.</summary>
public class SgMermaidOptions
{
    /// <summary>Mermaid theme. Default <see cref="SgMermaidTheme.Default"/>.</summary>
    public SgMermaidTheme Theme { get; set; } = SgMermaidTheme.Default;

    /// <summary>Font size in pixels. Default 14.</summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Security level for rendering.
    /// <c>"strict"</c> (default) — safest, no HTML in labels.
    /// <c>"loose"</c> — allows HTML in labels.
    /// <c>"antiscript"</c> — allows HTML but strips scripts.
    /// </summary>
    public string SecurityLevel { get; set; } = "strict";

    /// <summary>Start on load (auto-render). Default <c>false</c> — component controls rendering.</summary>
    public bool StartOnLoad { get; set; } = false;

    /// <summary>Flowchart curve style: "basis", "linear", "cardinal". Default "basis".</summary>
    public string FlowchartCurve { get; set; } = "basis";

    /// <summary>Sequence diagram: show actor mirrors at bottom. Default <c>false</c>.</summary>
    public bool SequenceMirrorActors { get; set; } = false;

    /// <summary>Maximum text length before Mermaid throws. Default 50000.</summary>
    public int MaxTextSize { get; set; } = 50000;
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed when a node is clicked in the diagram.</summary>
public class SgMermaidClickEventArgs
{
    /// <summary>ID of the clicked node.</summary>
    public string NodeId { get; set; } = string.Empty;
}
