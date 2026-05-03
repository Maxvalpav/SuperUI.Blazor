namespace SuperUI.Components;

/// <summary>
/// Defines the error-correction level for the <see cref="SgQrCode"/> component.
/// Higher levels allow more of the code to be damaged while still being readable,
/// but produce a denser QR image.
/// </summary>
public enum SgQrCodeEcc
{
    /// <summary>~7 % recovery capacity.</summary>
    Low,
    /// <summary>~15 % recovery capacity (default).</summary>
    Medium,
    /// <summary>~25 % recovery capacity.</summary>
    Quartile,
    /// <summary>~30 % recovery capacity (maximum).</summary>
    High
}
