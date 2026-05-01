using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// Single option for <see cref="SgSegmented{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class SgSegmentedOption<TValue>
{
    /// <summary>Value associated with the option.</summary>
    public TValue? Value { get; set; }

    /// <summary>Human-readable label shown on the segment.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>When true the option is rendered but cannot be selected.</summary>
    public bool Disabled { get; set; }

    /// <summary>Optional icon rendered before the label.</summary>
    public RenderFragment? Icon { get; set; }
}
