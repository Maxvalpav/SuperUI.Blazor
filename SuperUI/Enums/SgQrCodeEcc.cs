namespace SuperUI.Enums;

/// <summary>QR-code error-correction level.</summary>
public enum SgQrCodeEcc
{
    /// <summary>Low — ~7% damage recovery.</summary>
    Low = 0,
    /// <summary>Medium — ~15% damage recovery (default).</summary>
    Medium = 1,
    /// <summary>Quartile — ~25% damage recovery.</summary>
    Quartile = 2,
    /// <summary>High — ~30% damage recovery.</summary>
    High = 3
}
