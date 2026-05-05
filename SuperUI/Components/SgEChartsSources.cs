namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Apache ECharts bundle.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgEChartsSources { EChartsScript = "/lib/echarts/echarts.min.js" }
/// </code>
/// </example>
public sealed class SgEChartsSources
{
    /// <summary>
    /// ECharts UMD bundle (v5).
    /// Set to <c>null</c> if you load ECharts yourself via index.html.
    /// </summary>
    public string? EChartsScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/echarts@5.5.1/dist/echarts.min.js";
}
