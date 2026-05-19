using System;
using System.Collections.Generic;
using SuperUI.Enums;

namespace SuperUI.Components
{
    public class SgChartDataset
    {
        public string Label { get; set; } = "";
        public List<double> Data { get; set; } = new();
        public List<SgChartScatterPoint>? ScatterData { get; set; }
        public List<SgChartMatrixPoint>? MatrixData { get; set; }
        public string Color { get; set; } = "#2196f3";
        public List<string>? Colors { get; set; } // For Pie/Donut
        public string? FillColor { get; set; }
        public double BorderWidth { get; set; } = 2;
        public bool ShowPoints { get; set; } = true;
        public string? Stack { get; set; } // For stacked bar
        public SgChartType? Type { get; set; } // For mixed charts
    }

    public class SgChartScatterPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class SgChartMatrixPoint
    {
        public string X { get; set; } = "";
        public string Y { get; set; } = "";
        public double V { get; set; }
    }

    public class SgChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<SgChartDataset> Datasets { get; set; } = new();
    }

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

    /// <summary>Defines a named series projecting a numeric value out of TItem.</summary>
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

    public class SgChartClickEventArgs
    {
        public int DatasetIndex { get; set; }
        public int DataPointIndex { get; set; }
        public double Value { get; set; }
        public string? Label { get; set; }
    }

    public class SgChartXAxis
    {
        public string? Title { get; set; }
        public string Type { get; set; } = "category";
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool Display { get; set; } = true;
        public string? GridColor { get; set; }
        public bool ShowGrid { get; set; } = true;
    }

    public class SgChartYAxis
    {
        public string? Title { get; set; }
        public string Type { get; set; } = "linear";
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool Display { get; set; } = true;
        public string? GridColor { get; set; }
        public bool ShowGrid { get; set; } = true;
        public bool Primary { get; set; } = true;
    }
}
