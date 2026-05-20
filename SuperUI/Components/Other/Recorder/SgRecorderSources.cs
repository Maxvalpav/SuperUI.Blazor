namespace SuperUI.Components;

/// <summary>
/// Source URLs for the RecordRTC library.
/// Override to ship local copies or pin a specific version.
/// </summary>
public sealed class SgRecorderSources
{
    /// <summary>
    /// RecordRTC library URL.
    /// </summary>
    public string? RecordRtcScript { get; set; } =
        "https://unpkg.com/recordrtc@5.6.2/RecordRTC.min.js";
}
