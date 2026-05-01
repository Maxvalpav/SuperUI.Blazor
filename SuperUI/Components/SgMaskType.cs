namespace SuperUI.Components;

/// <summary>
/// Built-in mask presets for <see cref="SgMaskedInput"/>.
/// </summary>
public enum SgMaskType
{
    /// <summary>User-defined mask via <see cref="SgMaskedInput.Mask"/>.</summary>
    Custom,

    /// <summary>Phone number: +7 (999) 999-99-99.</summary>
    Phone,

    /// <summary>Russian INN: 10 digits.</summary>
    Inn,

    /// <summary>Russian KPP: 9 digits.</summary>
    Kpp,

    /// <summary>Bank account: 20 digits grouped by 4.</summary>
    Account,

    /// <summary>Currency with symbol and thousands separator.</summary>
    Currency,

    /// <summary>Percentage with % suffix.</summary>
    Percent
}
