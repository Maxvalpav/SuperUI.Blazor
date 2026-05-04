namespace SuperUI.Components;

/// <summary>
/// Represents a single row in an <see cref="SgVerticalGrid"/>.
/// </summary>
public sealed class SgVerticalGridRow
{
    /// <summary>Row label (left column).</summary>
    public string Label { get; set; } = "";

    /// <summary>Row value (right column). Can be any object — formatted via ToString() or <see cref="Format"/>.</summary>
    public object? Value { get; set; }

    /// <summary>Optional format string applied to <see cref="Value"/> (e.g. "C0", "dd.MM.yyyy").</summary>
    public string? Format { get; set; }

    /// <summary>Optional section name. Rows with the same section are grouped together.</summary>
    public string? Section { get; set; }

    /// <summary>Optional tooltip shown on the label.</summary>
    public string? Tooltip { get; set; }

    /// <summary>When true, the row is highlighted (e.g. changed or important value).</summary>
    public bool Highlighted { get; set; }

    /// <summary>When true, the row is shown in a muted/secondary style.</summary>
    public bool Muted { get; set; }

    /// <summary>When true, the row can be edited inline.</summary>
    public bool Editable { get; set; }

    /// <summary>Optional badge text shown next to the value.</summary>
    public string? BadgeText { get; set; }

    /// <summary>Badge variant. Default is <see cref="SgBadgeVariant.Info"/>.</summary>
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Info;

    /// <summary>Optional icon SVG markup shown before the label.</summary>
    public string? Icon { get; set; }

    /// <summary>Arbitrary tag for application use.</summary>
    public object? Tag { get; set; }

    /// <summary>Callback invoked when the value is edited inline.</summary>
    public Action<object?>? OnValueChanged { get; set; }
}
