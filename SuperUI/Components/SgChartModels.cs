using System;
using System.Collections.Generic;

namespace SuperUI.Components
{
    public enum SgChartType
    {
        Line,
        Bar,
        BarHorizontal,
        Area,
        Pie,
        Doughnut,
        Scatter,
        Heatmap
    }

    public class SgChartDataset
    {
        public string Label { get; set; } = "";
        public List<double> Data { get; set; } = new();
        public List<SgChartScatterPoint>? ScatterData { get; set; }
        public string Color { get; set; } = "#2196f3";
        public List<string>? Colors { get; set; } // For Pie/Donut
        public string? FillColor { get; set; }
        public double BorderWidth { get; set; } = 2;
        public bool ShowPoints { get; set; } = true;
        public string? Stack { get; set; } // For stacked bar
    }

    public class SgChartScatterPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
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
