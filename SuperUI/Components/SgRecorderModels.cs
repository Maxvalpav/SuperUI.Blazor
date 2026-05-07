namespace SuperUI.Components;

/// <summary>Supported MIME types for recording.</summary>
public enum SgRecorderMimeType
{
    /// <summary>WebM video (default).</summary>
    VideoWebM,
    /// <summary>WebM with VP9 codec.</summary>
    VideoWebM_VP9,
    /// <summary>WebM with H.264 codec.</summary>
    VideoWebM_H264,
    /// <summary>WAV audio.</summary>
    AudioWAV,
    /// <summary>OGG audio (Firefox only).</summary>
    AudioOGG
}

/// <summary>Recording state.</summary>
public enum SgRecorderState
{
    /// <summary>Idle, not recording.</summary>
    Idle,
    /// <summary>Requesting permissions.</summary>
    RequestingPermission,
    /// <summary>Permission granted, ready to record.</summary>
    Ready,
    /// <summary>Recording in progress.</summary>
    Recording,
    /// <summary>Recording paused.</summary>
    Paused,
    /// <summary>Stopped, finalizing.</summary>
    Stopping,
    /// <summary>Error state.</summary>
    Error
}

/// <summary>Event arguments when permissions are granted.</summary>
public class SgRecorderPermissionGrantedEventArgs : EventArgs
{
    /// <summary>List of available video devices.</summary>
    public List<SgVideoDevice> VideoDevices { get; set; } = new();
    /// <summary>List of available audio devices.</summary>
    public List<SgAudioDevice> AudioDevices { get; set; } = new();
}

/// <summary>Event arguments when recording starts.</summary>
public class SgRecorderStartedEventArgs : EventArgs
{
    /// <summary>Timestamp when recording started.</summary>
    public DateTime StartedAt { get; set; }
}

/// <summary>Event arguments when data chunk is available.</summary>
public class SgRecorderDataAvailableEventArgs : EventArgs
{
    /// <summary>Data chunk as base64 string.</summary>
    public string? DataBase64 { get; set; }
    /// <summary>Chunk size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>Timestamp.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>Event arguments when recording stops.</summary>
public class SgRecorderStoppedEventArgs : EventArgs
{
    /// <summary>Final recording as base64 data URL.</summary>
    public string? DataUrl { get; set; }
    /// <summary>Recording duration in seconds.</summary>
    public double Duration { get; set; }
    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>MIME type.</summary>
    public string? MimeType { get; set; }
}

/// <summary>Event arguments for error events.</summary>
public class SgRecorderErrorEventArgs : EventArgs
{
    /// <summary>Error message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Error type.</summary>
    public string? ErrorType { get; set; }
}

/// <summary>Represents an audio input device.</summary>
public class SgAudioDevice
{
    /// <summary>Device ID.</summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>Device label.</summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>Video constraints for the recorder.</summary>
public class SgVideoConstraints
{
    /// <summary>Video width.</summary>
    public int? Width { get; set; }
    /// <summary>Video height.</summary>
    public int? Height { get; set; }
    /// <summary>Frame rate.</summary>
    public double? FrameRate { get; set; }
    /// <summary>Aspect ratio.</summary>
    public double? AspectRatio { get; set; }
    /// <summary>Facing mode (user/environment).</summary>
    public string? FacingMode { get; set; }
}

/// <summary>Audio constraints for the recorder.</summary>
public class SgAudioConstraints
{
    /// <summary>Enable echo cancellation.</summary>
    public bool? EchoCancellation { get; set; }
    /// <summary>Enable noise suppression.</summary>
    public bool? NoiseSuppression { get; set; }
    /// <summary>Enable auto gain control.</summary>
    public bool? AutoGainControl { get; set; }
    /// <summary>Sample rate.</summary>
    public int? SampleRate { get; set; }
    /// <summary>Channel count (1 for mono, 2 for stereo).</summary>
    public int? ChannelCount { get; set; }
}
