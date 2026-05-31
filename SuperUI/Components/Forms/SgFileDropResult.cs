namespace SuperUI.Components;

/// <summary>Result of a file drop operation.</summary>
public class SgFileDropResult
{
    /// <summary>File name.</summary>
    public string Name { get; set; } = "";
    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>MIME type.</summary>
    public string Type { get; set; } = "";
    /// <summary>File content as a data URL (base64).</summary>
    public string? DataUrl { get; set; }
}
