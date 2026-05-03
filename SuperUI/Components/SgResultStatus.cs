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
    /// <summary>404 Not Found state.</summary>
    Status404
}
