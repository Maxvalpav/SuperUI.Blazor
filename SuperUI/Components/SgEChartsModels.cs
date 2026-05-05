namespace SuperUI.Components;

// ── Chart type ────────────────────────────────────────────────────────────────

/// <summary>Supported ECharts chart types.</summary>
public enum SgEChartsType
{
    Line,
    Bar,
    BarHorizontal,
    Area,
    Pie,
    Donut,
    Scatter,
    Radar,
    Heatmap,
    Gauge,
    Funnel,
    Sankey,
    Tree,
    Sunburst,
    Candlestick,
}

// ── Data models ───────────────────────────────────────────────────────────────

/// <summary>A single labelled numeric data point.</summary>
public class SgEChartsDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Group { get; set; }
    public string? Color { get; set; }
}

/// <summary>A node in a Sankey or Tree chart.</summary>
public class SgEChartsNode
{
    public string Id   { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? Category { get; set; }
    public List<SgEChartsNode>? Children { get; set; }
}

/// <summary>A link between two nodes (Sankey).</summary>
public class SgEChartsLink
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public double Value  { get; set; } = 1;
}

/// <summary>Graph data for Sankey / Tree charts.</summary>
public class SgEChartsGraphData
{
    public List<SgEChartsNode> Nodes { get; set; } = new();
    public List<SgEChartsLink> Links { get; set; } = new();
}

/// <summary>A candlestick OHLC data point.</summary>
public class SgEChartsCandlePoint
{
    public string Date  { get; set; } = string.Empty;
    public double Open  { get; set; }
    public double Close { get; set; }
    public double Low   { get; set; }
    public double High  { get; set; }
}

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgECharts"/>.</summary>
public class SgEChartsOptions
{
    /// <summary>Show legend. Default <c>true</c>.</summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>Show grid lines. Default <c>true</c>.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Show axis labels. Default <c>true</c>.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Enable zoom (dataZoom). Default <c>false</c>.</summary>
    public bool EnableZoom { get; set; } = false;

    /// <summary>Animate on first render. Default <c>true</c>.</summary>
    public bool Animate { get; set; } = true;

    /// <summary>Animation duration in ms. Default 800.</summary>
    public int AnimationDuration { get; set; } = 800;

    /// <summary>Optional units suffix for tooltips (e.g. "%", " ms").</summary>
    public string? ValueSuffix { get; set; }

    /// <summary>Decimal places for tooltip values. <c>null</c> = auto.</summary>
    public int? ValueDecimals { get; set; }

    /// <summary>Show built-in toolbar (save image, zoom reset). Default <c>false</c>.</summary>
    public bool ShowToolbar { get; set; } = false;

    /// <summary>Stack series. Default <c>false</c>.</summary>
    public bool Stacked { get; set; } = false;

    /// <summary>Smooth lines (line/area). Default <c>false</c>.</summary>
    public bool Smooth { get; set; } = false;

    /// <summary>Show data point markers. Default <c>true</c>.</summary>
    public bool ShowPoints { get; set; } = true;

    /// <summary>Custom colour palette. <c>null</c> = ECharts default.</summary>
    public List<string>? Colors { get; set; }

    /// <summary>Gauge min value. Default 0.</summary>
    public double GaugeMin { get; set; } = 0;

    /// <summary>Gauge max value. Default 100.</summary>
    public double GaugeMax { get; set; } = 100;

    /// <summary>Inner radius ratio for Donut (0–1). Default 0.5.</summary>
    public double DonutInnerRadius { get; set; } = 0.5;

    /// <summary>Background colour. <c>null</c> = transparent.</summary>
    public string? BackgroundColor { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed to <see cref="SgECharts.OnDataPointClick"/>.</summary>
public class SgEChartsClickEventArgs
{
    public string SeriesName { get; set; } = string.Empty;
    public string Name       { get; set; } = string.Empty;
    public double Value      { get; set; }
    public int    DataIndex  { get; set; }
    public int    SeriesIndex{ get; set; }
}
