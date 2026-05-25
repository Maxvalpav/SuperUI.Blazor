namespace SuperUI.Components;

public sealed class SgTimeSeriesPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

public sealed class SgAnomaly
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double MaxDeviation { get; set; }
    public string Type { get; set; } = "spike";
    public string Description { get; set; } = string.Empty;
}

public sealed class SgAnomalyDetectionResult
{
    public List<SgAnomaly> Anomalies { get; set; } = new();
    public double Mean { get; set; }
    public double StdDev { get; set; }
    public int TotalPoints { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
