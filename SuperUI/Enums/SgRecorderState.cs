namespace SuperUI.Enums;

/// <summary>Recording state of the media recorder.</summary>
public enum SgRecorderState
{
    /// <summary>Initial / idle state, before any permission request.</summary>
    Idle = 0,
    /// <summary>Permission request is in progress.</summary>
    RequestingPermission = 1,
    /// <summary>Permission granted and device ready.</summary>
    Ready = 2,
    /// <summary>Currently recording.</summary>
    Recording = 3,
    /// <summary>Recording paused.</summary>
    Paused = 4,
    /// <summary>Waiting for stop to complete.</summary>
    Stopping = 5,
    /// <summary>An error occurred.</summary>
    Error = 6
}
