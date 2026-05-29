namespace SuperUI.Components;

/// <summary>A single time-series data point for anomaly detection.</summary>
public sealed class SgTimeSeriesPoint
{
    /// <summary>Timestamp of the data point.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Numeric value at this timestamp.</summary>
    public double Value { get; set; }
}

/// <summary>An anomalous region detected in the time series.</summary>
public sealed class SgAnomaly
{
    /// <summary>Starting index of the anomaly in the data array.</summary>
    public int StartIndex { get; set; }
    /// <summary>Ending index of the anomaly in the data array.</summary>
    public int EndIndex { get; set; }
    /// <summary>Start timestamp of the anomaly.</summary>
    public DateTime StartTime { get; set; }
    /// <summary>End timestamp of the anomaly.</summary>
    public DateTime EndTime { get; set; }
    /// <summary>Maximum Z-score deviation within this anomaly region.</summary>
    public double MaxDeviation { get; set; }
    /// <summary>Anomaly classification type (extreme, spike, deviation).</summary>
    public string Type { get; set; } = "spike";
    /// <summary>Human-readable description of the anomaly.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>Result of anomaly detection on a time series.</summary>
public sealed class SgAnomalyDetectionResult
{
    /// <summary>List of detected anomalies.</summary>
    public List<SgAnomaly> Anomalies { get; set; } = new();
    /// <summary>Global mean of the time-series values.</summary>
    public double Mean { get; set; }
    /// <summary>Global standard deviation of the time-series values.</summary>
    public double StdDev { get; set; }
    /// <summary>Total number of data points analyzed.</summary>
    public int TotalPoints { get; set; }
    /// <summary>Time taken to run the anomaly detection algorithm.</summary>
    public TimeSpan ProcessingTime { get; set; }
}
