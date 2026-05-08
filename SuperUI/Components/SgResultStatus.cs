namespace SuperUI.Components;

/// <summary>
/// Defines the status type for the <see cref="SgResult"/> component.
/// </summary>
public enum SgResultStatus
{
    /// <summary>Success state (green check).</summary>
    Success,
    /// <summary>Error state (red cross).</summary>
    Error,
    /// <summary>Informational state (blue info icon).</summary>
    Info,
    /// <summary>Warning state (yellow warning icon).</summary>
    Warning,
    /// <summary>403 Forbidden state.</summary>
    Status403,
    /// <summary>404 Not Found state.</summary>
    Status404,
    /// <summary>500 Internal Server Error state.</summary>
    Status500
}
