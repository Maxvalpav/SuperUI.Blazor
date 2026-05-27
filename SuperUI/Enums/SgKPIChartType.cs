namespace SuperUI.Enums;

/// <summary>Chart style for <c>SgKPICard</c> sparkline / wave area.</summary>
public enum SgKPIChartType
{
    /// <summary>Standard polyline connecting data points.</summary>
    Line,
    /// <summary>Filled area under a smooth curve (default).</summary>
    Area,
    /// <summary>Vertical bars for each data point.</summary>
    Bar,
    /// <summary>Step-like line chart with right-angle transitions.</summary>
    Step,
    /// <summary>Simplified candlestick chart — green/red rects per adjacent pair.</summary>
    Candle,
    /// <summary>Compact bar sparkline (renders identically to Bar).</summary>
    SparkBar,
    /// <summary>Compact line sparkline (renders identically to Line).</summary>
    SparkLine,
    /// <summary>Compact area sparkline (renders identically to Area).</summary>
    SparkArea
}
