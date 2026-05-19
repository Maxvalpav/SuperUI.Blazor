namespace SuperUI.Enums;

/// <summary>KaTeX / barcode input format.</summary>
public enum SgBarcodeFormat
{
    /// <summary>AUTO — detect automatically.</summary>
    Auto = 0,
    /// <summary>EAN-13</summary>
    Ean13 = 1,
    /// <summary>EAN-8</summary>
    Ean8 = 2,
    /// <summary>UPC-E</summary>
    UpcE = 3,
    /// <summary>Code-39</summary>
    Code39 = 4,
    /// <summary>Code-128</summary>
    Code128 = 5,
    /// <summary>Code-93</summary>
    Code93 = 6,
    /// <summary>Codabar</summary>
    Codabar = 7,
    /// <summary>ITF (Interleaved 2-of-5)</summary>
    Itf = 8,
    /// <summary>QR Code</summary>
    Qr = 9,
    /// <summary>Data Matrix</summary>
    DataMatrix = 10,
    /// <summary>Aztec code.</summary>
    Aztec = 11,
    /// <summary>PDF417</summary>
    Pdf417 = 12,
    /// <summary>All formats (scan everything).</summary>
    All = 999
}
