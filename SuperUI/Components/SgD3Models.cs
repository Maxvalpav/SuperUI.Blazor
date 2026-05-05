namespace SuperUI.Components;

// ── Chart type ────────────────────────────────────────────────────────────────

/// <summary>Supported D3 visualisation types.</summary>
public enum SgD3ChartType
{
    /// <summary>Vertical bar chart.</summary>
    Bar,
    /// <summary>Horizontal bar chart.</summary>
    BarHorizontal,
    /// <summary>Line chart (single or multi-series).</summary>
    Line,
    /// <summary>Area chart with fill.</summary>
    Area,
    /// <summary>Pie chart.</summary>
    Pie,
    /// <summary>Donut chart.</summary>
    Donut,
    /// <summary>Scatter / bubble plot.</summary>
    Scatter,
    /// <summary>Force-directed network graph.</summary>
    ForceGraph,
    /// <summary>Treemap.</summary>
    Treemap,
    /// <summary>Radial / spider chart.</summary>
    Radar,
}

// ── Data models ───────────────────────────────────────────────────────────────

/// <summary>A single labelled numeric data point.</summary>
public class SgD3DataPoint
{
    /// <summary>Category label shown on the axis.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Numeric value.</summary>
    public double Value { get; set; }

    /// <summary>Optional group / series name used for multi-series charts.</summary>
    public string? Group { get; set; }
}

/// <summary>A node in a force-directed graph or treemap hierarchy.</summary>
public class SgD3Node
{
    /// <summary>Unique node identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional numeric value (used for node size / treemap area).</summary>
    public double Value { get; set; }

    /// <summary>Optional group for colour coding.</summary>
    public string? Group { get; set; }
}

/// <summary>A directed or undirected edge between two nodes.</summary>
public class SgD3Link
{
    /// <summary>Source node ID.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Target node ID.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Optional edge weight.</summary>
    public double Value { get; set; } = 1;
}

/// <summary>Graph data for <see cref="SgD3ChartType.ForceGraph"/>.</summary>
public class SgD3GraphData
{
    public List<SgD3Node> Nodes { get; set; } = new();
    public List<SgD3Link> Links { get; set; } = new();
}

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgD3Chart"/>.</summary>
public class SgD3Options
{
    /// <summary>Show axis grid lines. Default <c>true</c>.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Show axis tick labels. Default <c>true</c>.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Show legend. Default <c>true</c>.</summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>Enable zoom and pan via scroll / drag. Default <c>false</c>.</summary>
    public bool EnableZoom { get; set; } = false;

    /// <summary>Animate on first render. Default <c>true</c>.</summary>
    public bool Animate { get; set; } = true;

    /// <summary>Animation duration in ms. Default 600.</summary>
    public int AnimationDuration { get; set; } = 600;

    /// <summary>
    /// Explicit colour palette. When <c>null</c> the component uses its built-in
    /// 10-colour scheme that respects the active SuperUI theme.
    /// </summary>
    public List<string>? Colors { get; set; }

    /// <summary>Optional units suffix appended to tooltip values (e.g. "%", " ms").</summary>
    public string? ValueSuffix { get; set; }

    /// <summary>Decimal places for tooltip / axis values. <c>null</c> = auto.</summary>
    public int? ValueDecimals { get; set; }

    /// <summary>Show a built-in toolbar (zoom reset, export SVG/PNG). Default <c>false</c>.</summary>
    public bool ShowToolbar { get; set; } = false;

    /// <summary>Inner radius ratio for <see cref="SgD3ChartType.Donut"/> (0–1). Default 0.55.</summary>
    public double DonutInnerRadius { get; set; } = 0.55;

    /// <summary>
    /// Curve type for line / area charts.
    /// Accepted values: <c>"linear"</c>, <c>"monotone"</c>, <c>"step"</c>, <c>"basis"</c>.
    /// Default <c>"monotone"</c>.
    /// </summary>
    public string Curve { get; set; } = "monotone";

    /// <summary>Show data point circles on line / area charts. Default <c>true</c>.</summary>
    public bool ShowPoints { get; set; } = true;

    /// <summary>Stack bar / area series. Default <c>false</c>.</summary>
    public bool Stacked { get; set; } = false;

    /// <summary>Force-graph link distance. Default 80.</summary>
    public double ForceDistance { get; set; } = 80;

    /// <summary>Force-graph charge strength (negative = repulsion). Default -200.</summary>
    public double ForceCharge { get; set; } = -200;
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed to <see cref="SgD3Chart.OnDataPointClick"/>.</summary>
public class SgD3ClickEventArgs
{
    /// <summary>Label of the clicked element.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Numeric value of the clicked element.</summary>
    public double Value { get; set; }

    /// <summary>Series / group name, if applicable.</summary>
    public string? Group { get; set; }

    /// <summary>Zero-based index within the dataset.</summary>
    public int Index { get; set; }
}
