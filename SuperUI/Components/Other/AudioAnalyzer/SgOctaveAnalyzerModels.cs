namespace SuperUI.Components;

public enum SgWeightingCurve { A, C, Z }

public sealed class SgOctaveBand
{
    public double CenterFreq { get; set; }   // Hz: 31.5, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000
    public string Label { get; set; } = string.Empty;
    public double RawDb { get; set; }
    public double WeightedDb { get; set; }
    public double PeakDb { get; set; }
}

public sealed class SgOctaveReading
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<SgOctaveBand> Bands { get; set; } = new();
    public double OverallDb { get; set; }
    public SgWeightingCurve Weighting { get; set; }
}
