namespace SuperUI.Components;

/// <summary>
/// A secondary comparison metric displayed in the delta row of
/// <c>SgKPICard</c> Ban and Analytic modes.
/// </summary>
public sealed class SgKPIDelta
{
    /// <summary>Short label, e.g. "vs. PY (%)" or "vs. PY (#)".</summary>
    public string? Label { get; init; }

    /// <summary>Numeric delta value (positive or negative).</summary>
    public double Value { get; init; }

    /// <summary>When true the value is formatted as a percentage.</summary>
    public bool IsPercent { get; init; }

    /// <summary>Invert color logic — negative is good (e.g. cost reduction).</summary>
    public bool Invert { get; init; }
}
