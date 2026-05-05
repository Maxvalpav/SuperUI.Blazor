namespace SuperUI.Components;

// ── Diagram type ──────────────────────────────────────────────────────────────

/// <summary>Mermaid diagram type hint (used for validation and tooling only — the actual type is inferred from the definition string).</summary>
public enum SgMermaidDiagramType
{
    /// <summary>Flowchart / graph.</summary>
    Flowchart,
    /// <summary>Sequence diagram.</summary>
    Sequence,
    /// <summary>Class diagram.</summary>
    ClassDiagram,
    /// <summary>State diagram.</summary>
    StateDiagram,
    /// <summary>Entity-relationship diagram.</summary>
    ErDiagram,
    /// <summary>Gantt chart.</summary>
    Gantt,
    /// <summary>Pie chart.</summary>
    Pie,
    /// <summary>Git graph.</summary>
    GitGraph,
    /// <summary>Journey / user journey.</summary>
    Journey,
    /// <summary>Mindmap.</summary>
    Mindmap,
    /// <summary>Timeline.</summary>
    Timeline,
    /// <summary>Quadrant chart.</summary>
    Quadrant,
    /// <summary>C4 context diagram.</summary>
    C4Context,
    /// <summary>Custom / unknown type.</summary>
    Custom,
}

// ── Theme ─────────────────────────────────────────────────────────────────────

/// <summary>Mermaid built-in theme.</summary>
public enum SgMermaidTheme
{
    Default,
    Dark,
    Forest,
    Neutral,
    Base,
}

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
