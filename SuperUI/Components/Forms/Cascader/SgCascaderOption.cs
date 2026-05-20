namespace SuperUI.Components;

/// <summary>
/// Single option for <see cref="SgCascader"/>.
/// </summary>
public sealed class SgCascaderOption
{
    /// <summary>Unique value of this option.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Display label for this option.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Child options for the next cascade level.</summary>
    public List<SgCascaderOption> Children { get; set; } = new();

    /// <summary>When true the option cannot be selected.</summary>
    public bool Disabled { get; set; }
}
