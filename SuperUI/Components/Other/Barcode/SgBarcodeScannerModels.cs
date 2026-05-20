using SuperUI.Enums;

namespace SuperUI.Components;

// ── Supported barcode/QR code formats ──────────────────────────────────────────
// SgBarcodeFormat — moved to SuperUI.Enums.SgBarcodeFormat

// ── Event arguments ───────────────────────────────────────────────────────────
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
