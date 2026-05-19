namespace SuperUI.Enums;

/// <summary>MIME type / container format for the media recorder.</summary>
public enum SgRecorderMimeType
{
    /// <summary>Video / WebM (VP8 codec).</summary>
    VideoWebM = 0,
    /// <summary>Video / WebM (VP9 codec).</summary>
    VideoWebM_VP9 = 1,
    /// <summary>Video / WebM (H.264 codec).</summary>
    VideoWebM_H264 = 2,
    /// <summary>Audio / WAV.</summary>
    AudioWAV = 3,
    /// <summary>Audio / OGG.</summary>
    AudioOGG = 4
}
