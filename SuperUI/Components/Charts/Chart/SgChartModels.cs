using System;
using System.Collections.Generic;
using SuperUI.Enums;

namespace SuperUI.Components
{
    /// <summary>Represents a single dataset within a chart, containing data points and visual configuration.</summary>
    public class SgChartDataset
    {
        /// <summary>Gets or sets the display label for the dataset (used in legends).</summary>
        public string Label { get; set; } = "";
        /// <summary>Gets or sets the numeric data values for the dataset.</summary>
        public List<double> Data { get; set; } = new();
        /// <summary>Gets or sets optional scatter (X,Y) data points for scatter charts.</summary>
        public List<SgChartScatterPoint>? ScatterData { get; set; }
        /// <summary>Gets or sets optional matrix data points for heatmap-style charts.</summary>
        public List<SgChartMatrixPoint>? MatrixData { get; set; }
        /// <summary>Gets or sets the primary color for the dataset.</summary>
        public string Color { get; set; } = "#2196f3";
        /// <summary>Gets or sets individual colors per data point (used for Pie/Donut charts).</summary>
        public List<string>? Colors { get; set; }
        /// <summary>Gets or sets the fill color for area-style datasets.</summary>
        public string? FillColor { get; set; }
        /// <summary>Gets or sets the border width for line-based datasets.</summary>
        public double BorderWidth { get; set; } = 2;
        /// <summary>Gets or sets whether to show data points on line/area charts.</summary>
        public bool ShowPoints { get; set; } = true;
        /// <summary>Gets or sets the stack group identifier for stacked bar/area charts.</summary>
        public string? Stack { get; set; }
        /// <summary>Gets or sets the chart type override for mixed charts (e.g. a bar dataset in a line chart).</summary>
        public SgChartType? Type { get; set; }
    }

    /// <summary>Represents a single point in a scatter chart with X and Y coordinates.</summary>
    public class SgChartScatterPoint
    {
        /// <summary>Gets or sets the X-axis value.</summary>
        public double X { get; set; }
        /// <summary>Gets or sets the Y-axis value.</summary>
        public double Y { get; set; }
    }

    /// <summary>Represents a single cell in a matrix/heatmap chart with string-based X and Y keys and a numeric value.</summary>
    public class SgChartMatrixPoint
    {
        /// <summary>Gets or sets the X-axis (column) key.</summary>
        public string X { get; set; } = "";
        /// <summary>Gets or sets the Y-axis (row) key.</summary>
        public string Y { get; set; } = "";
        /// <summary>Gets or sets the numeric value at this matrix position.</summary>
        public double V { get; set; }
    }

    /// <summary>Represents the complete data structure for a chart, including labels and datasets.</summary>
    public class SgChartData
    {
        /// <summary>Gets or sets the category labels displayed along the X-axis.</summary>
        public List<string> Labels { get; set; } = new();
        /// <summary>Gets or sets the collection of datasets to render on the chart.</summary>
        public List<SgChartDataset> Datasets { get; set; } = new();
    }

    /// <summary>Configuration options for chart appearance and behavior.</summary>
    public class SgChartOptions
    {
        public bool ShowGrid { get; set; } = true;
        public bool ShowLabels { get; set; } = true;
        public bool ShowLegend { get; set; } = true;
        public bool Responsive { get; set; } = true;
        public string Height { get; set; } = "300px";
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public bool EnableZoom { get; set; } = true;
        public bool EnableDecimation { get; set; } = true;
        public int DecimationThreshold { get; set; } = 10000;
        public int? DecimationTargetPoints { get; set; } = 1000;
        public string BorderColor { get; set; } = "#2196f3";
        public string BackgroundColor { get; set; } = "rgba(33, 150, 243, 0.5)";
        public List<string>? Colors { get; set; }

        /// <summary>Smooths line tension; 0 = straight segments, ~0.4 = soft curve.</summary>
        public double LineTension { get; set; } = 0.35;

        /// <summary>Render points on line/area datasets.</summary>
        public bool ShowPoints { get; set; } = true;

        /// <summary>Shows values directly on chart points.</summary>
        public bool ShowDataLabels { get; set; }

        /// <summary>Decimal places for data label values. Default 2.</summary>
        public int DataLabelDecimals { get; set; } = 2;

        /// <summary>Suffix appended to data label values (e.g. "₽", "%").</summary>
        public string? DataLabelSuffix { get; set; }

        /// <summary>
        /// Show label every N-th point. 0 = auto (based on dataset size).
        /// Use 1 to show all, 5 to show every 5th, etc.
        /// </summary>
        public int DataLabelStep { get; set; } = 0;

        /// <summary>Stack bar/area datasets along the value axis.</summary>
        public bool Stacked { get; set; }

        /// <summary>Render a built-in toolbar (zoom reset, export PNG).</summary>
        public bool ShowToolbar { get; set; }

        /// <summary>Animation duration in ms. 0 disables animations.</summary>
        public int AnimationDuration { get; set; } = 400;

        /// <summary>Optional units suffix appended to tick / tooltip values (e.g. "%", " ms").</summary>
        public string? ValueSuffix { get; set; }

        /// <summary>Optional decimals for tooltip / tick formatting.</summary>
        public int? ValueDecimals { get; set; }

        /// <summary>When true, hover highlights the whole index across datasets.</summary>
        public bool SharedTooltip { get; set; } = true;
    }

    /// <summary>Defines a named series that projects a numeric value from TItem for data-driven charts.</summary>
    public class SgChartSeries<TItem>
    {
        public string Name { get; set; } = "";
        public Func<TItem, double> Value { get; set; } = _ => 0;
        public SgChartType? Type { get; set; }

        public SgChartSeries() { }

        public SgChartSeries(string name, Func<TItem, double> value, SgChartType? type = null)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }

    /// <summary>Provides data about a chart click event, including the dataset and data point indices.</summary>
    public class SgChartClickEventArgs
    {
        /// <summary>Gets or sets the index of the clicked dataset.</summary>
        public int DatasetIndex { get; set; }
        /// <summary>Gets or sets the index of the clicked data point within its dataset.</summary>
        public int DataPointIndex { get; set; }
        /// <summary>Gets or sets the numeric value of the clicked data point.</summary>
        public double Value { get; set; }
        /// <summary>Gets or sets the label of the clicked data point.</summary>
        public string? Label { get; set; }
    }

    /// <summary>Configuration for the X-axis of a chart.</summary>
    /// <summary>Configuration for the X-axis of a chart.</summary>
    public class SgChartXAxis
    {
        /// <summary>Gets or sets the axis title text.</summary>
        public string? Title { get; set; }
        /// <summary>Gets or sets the axis type (e.g. "category", "linear", "time").</summary>
        public string Type { get; set; } = "category";
        /// <summary>Gets or sets the minimum value displayed on the axis.</summary>
        public double? Min { get; set; }
        /// <summary>Gets or sets the maximum value displayed on the axis.</summary>
        public double? Max { get; set; }
        /// <summary>Gets or sets whether the axis is displayed.</summary>
        public bool Display { get; set; } = true;
        /// <summary>Gets or sets the color of the grid lines for this axis.</summary>
        public string? GridColor { get; set; }
        /// <summary>Gets or sets whether grid lines are shown for this axis.</summary>
        public bool ShowGrid { get; set; } = true;
    }

    /// <summary>Configuration for the Y-axis of a chart.</summary>
    public class SgChartYAxis
    {
        /// <summary>Gets or sets the axis title text.</summary>
        public string? Title { get; set; }
        /// <summary>Gets or sets the axis type (e.g. "linear", "logarithmic").</summary>
        public string Type { get; set; } = "linear";
        /// <summary>Gets or sets the minimum value displayed on the axis.</summary>
        public double? Min { get; set; }
        /// <summary>Gets or sets the maximum value displayed on the axis.</summary>
        public double? Max { get; set; }
        /// <summary>Gets or sets whether the axis is displayed.</summary>
        public bool Display { get; set; } = true;
        /// <summary>Gets or sets the color of the grid lines for this axis.</summary>
        public string? GridColor { get; set; }
        /// <summary>Gets or sets whether grid lines are shown for this axis.</summary>
        public bool ShowGrid { get; set; } = true;
        /// <summary>Gets or sets whether this is the primary Y-axis (left) or secondary (right).</summary>
        public bool Primary { get; set; } = true;
    }
}
