namespace SuperUI.Components;

/// <summary>Visual variant for <see cref="SgCard"/>.</summary>
public enum SgCardVariant
{
    /// <summary>Default surface with light shadow.</summary>
    Default,
    /// <summary>Elevated surface with stronger shadow (no border).</summary>
    Elevated,
    /// <summary>Outlined card with border and no shadow.</summary>
    Outlined,
    /// <summary>Filled with secondary background, no shadow.</summary>
    Filled,
    /// <summary>Transparent card without background or border.</summary>
    Ghost
}

/// <summary>Optional status accent shown as a left stripe on a <see cref="SgCard"/>.</summary>
public enum SgCardStatus
{
    None,
    Info,
    Success,
    Warning,
    Danger,
    Muted
}
