namespace SuperUI.Components;

/// <summary>
/// Single option for <see cref="SgRadioGroup{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class RadioOption<TValue>
{
    /// <summary>Value associated with the option.</summary>
    public TValue? Value { get; set; }

    /// <summary>Human-readable label shown next to the radio.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>When true the option is rendered but cannot be selected.</summary>
    public bool Disabled { get; set; }
}
