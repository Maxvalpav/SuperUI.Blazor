namespace SuperUI.Components;

/// <summary>Supported barcode/QR code formats.</summary>
public enum SgBarcodeFormat
{
    /// <summary>QR Code</summary>
    QRCode,
    /// <summary>Code 128</summary>
    Code128,
    /// <summary>Code 39</summary>
    Code39,
    /// <summary>EAN-13</summary>
    EAN13,
    /// <summary>EAN-8</summary>
    EAN8,
    /// <summary>UPC-A</summary>
    UPCA,
    /// <summary>UPC-E</summary>
    UPCE,
    /// <summary>ITF</summary>
    ITF,
    /// <summary>PDF417</summary>
    PDF417,
    /// <summary>Data Matrix</summary>
    DataMatrix,
    /// <summary>Aztec</summary>
    Aztec,
    /// <summary>Codabar</summary>
    Codabar,
    /// <summary>All supported formats</summary>
    All
}

/// <summary>Event arguments for barcode/QR code received event.</summary>
public class SgBarcodeReceivedEventArgs : EventArgs
{
    /// <summary>Scanned barcode/QR code text.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Format of the scanned code.</summary>
    public string Format { get; set; } = string.Empty;
    /// <summary>Base64 encoded image of the scanned frame (if enabled).</summary>
    public string? Picture { get; set; }
}

/// <summary>Event arguments for device list changed event.</summary>
public class SgDeviceListChangedEventArgs : EventArgs
{
    /// <summary>List of available video devices.</summary>
    public List<SgVideoDevice> Devices { get; set; } = new();
}

/// <summary>Represents a video input device.</summary>
public class SgVideoDevice
{
    /// <summary>Device ID.</summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>Device label.</summary>
    public string Label { get; set; } = string.Empty;
}
