namespace SuperUI.Core;

/// <summary>
/// Shared lifecycle / async state used by data, form, and overlay components
/// to drive loading / empty / error slots uniformly.
/// </summary>
public enum SgState
{
    /// <summary>Idle / nothing happening.</summary>
    Idle,
    /// <summary>Async work in progress.</summary>
    Loading,
    /// <summary>Async work completed successfully.</summary>
    Success,
    /// <summary>No data available.</summary>
    Empty,
    /// <summary>Async work failed.</summary>
    Error
}
