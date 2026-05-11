namespace SuperUI.Core;

/// <summary>
/// Generates DOM-safe, stable, monotonically increasing identifiers.
/// Useful for <c>aria-controls</c>/<c>aria-labelledby</c> wiring and any element id
/// that must remain stable across re-renders.
/// </summary>
/// <remarks>
/// The default static <see cref="Next"/> uses a process-wide counter. Inject
/// <see cref="SgIdGenerator"/> when you need an instance scoped to a circuit / session,
/// which prevents id collisions in tests and aids SSR-friendly hydration matching.
/// </remarks>
public sealed class SgIdGenerator
{
    private static long _global;
    private long _local;

    /// <summary>Returns the next id using the process-wide counter.</summary>
    public static string Next(string prefix = "sg")
        => $"{prefix}-{Interlocked.Increment(ref _global):x}";

    /// <summary>Returns the next id from this instance's counter.</summary>
    public string NewId(string prefix = "sg")
        => $"{prefix}-{Interlocked.Increment(ref _local):x}";

    /// <summary>Resets this instance's counter. Test-only.</summary>
    public void Reset() => Interlocked.Exchange(ref _local, 0);
}
