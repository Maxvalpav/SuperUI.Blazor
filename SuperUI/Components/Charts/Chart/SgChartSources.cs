namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Chart.js bundle and optional plugin scripts.
/// Override to ship local copies or pin specific versions.
/// </summary>
/// <example>
/// Use local files:
/// <code>
/// new SgChartSources
/// {
///     ChartScript = "/lib/chart.js/chart.umd.min.js"
/// }
/// </code>
/// Disable zoom plugin:
/// <code>
/// new SgChartSources { ZoomScript = null }
/// </code>
/// </example>
public sealed class SgChartSources
{
    /// <summary>
    /// UMD bundle for Chart.js.
    /// Set to <c>null</c> if you load Chart.js yourself (e.g. via index.html).
    /// </summary>
    public string? ChartScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/chart.js@4.5.1/dist/chart.umd.min.js";

    /// <summary>
    /// chartjs-plugin-zoom — enables wheel/pinch zoom and pan via Ctrl+wheel / Shift+drag.
    /// Set to <c>null</c> to disable the zoom plugin entirely.
    /// </summary>
    public string? ZoomScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/chartjs-plugin-zoom@2.2.0/dist/chartjs-plugin-zoom.min.js";

    /// <summary>
    /// chartjs-chart-matrix — adds the Heatmap chart type.
    /// Set to <c>null</c> if you don't use <see cref="SgChartType.Heatmap"/>.
    /// </summary>
    public string? MatrixScript { get; set; } =
        "https://cdn.jsdelivr.net/npm/chartjs-chart-matrix@2.0.0/dist/chartjs-chart-matrix.min.js";
}
