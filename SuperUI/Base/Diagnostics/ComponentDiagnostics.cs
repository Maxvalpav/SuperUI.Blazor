// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Per-component diagnostics object. Tracks render counts, times, errors, and parameter changes.
/// Used by SgComponentBase for DEBUG builds.
/// </summary>
public class ComponentDiagnostics : IDisposable
{
    public string ComponentId { get; set; } = string.Empty;
    public int RenderCount { get; set; }
    public double LastRenderMs { get; set; }
    public double MaxRenderMs { get; set; }
    public double AverageRenderMs { get; set; }
    public int ParameterChangeCount { get; set; }
    public int ErrorCount { get; set; }

    public void RecordError(string componentId, Exception exception)
    {
        ErrorCount++;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Collects and exposes component diagnostics data: render counts,
/// render times, errors, parameter changes. Implements
/// ISgDiagnosticsCollector for DI registration.
/// </summary>
public class SgDiagnosticsCollector : ISgDiagnosticsCollector, IDisposable
{
    private readonly ConcurrentDictionary<string, ComponentDiagnosticEntry> _entries = new();
    private readonly ConcurrentQueue<ComponentErrorRecord> _errors = new();
    private int _totalRenderCount;
    private long _totalRenderTicks;

    // --- ISgDiagnosticsCollector implementation ---

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, ComponentDiagnosticEntry> Entries => _entries;

    /// <inheritdoc/>
    public int TotalRenderCount => _totalRenderCount;

    /// <inheritdoc/>
    public TimeSpan TotalRenderTime => TimeSpan.FromTicks(Interlocked.Read(ref _totalRenderTicks));

    /// <inheritdoc/>
    public int ErrorCount => _errors.Count;

    // --- Render tracking ---

    /// <summary>Record a render event for a component.</summary>
    public void RecordRender(string componentId, long elapsedTicks)
    {
        var entry = _entries.GetOrAdd(componentId, _ => new ComponentDiagnosticEntry(componentId));
        entry.RecordRender(elapsedTicks);
        Interlocked.Increment(ref _totalRenderCount);
        Interlocked.Add(ref _totalRenderTicks, elapsedTicks);
    }

    /// <summary>Record a parameter change for a component.</summary>
    public void RecordParameterChange(string componentId, string parameterName)
    {
        var entry = _entries.GetOrAdd(componentId, _ => new ComponentDiagnosticEntry(componentId));
        entry.RecordParameterChange(parameterName);
    }

    /// <summary>Record a component error.</summary>
    public void RecordError(string componentId, Exception exception)
    {
        var entry = _entries.GetOrAdd(componentId, _ => new ComponentDiagnosticEntry(componentId));
        entry.RecordError();
        _errors.Enqueue(new ComponentErrorRecord(componentId, exception, DateTimeOffset.UtcNow));
    }

    /// <summary>Get all recorded errors (most recent first).</summary>
    public IReadOnlyCollection<ComponentErrorRecord> GetErrors()
    {
        return _errors.ToArray();
    }

    /// <summary>Get diagnostic summary as formatted text.</summary>
    public string GetSummary()
    {
        return $"Components: {_entries.Count}, Total Renders: {_totalRenderCount}, " +
               $"Render Time: {TotalRenderTime.TotalMilliseconds:F2}ms, Errors: {_errors.Count}";
    }

    /// <summary>Reset all collected data.</summary>
    public void Reset()
    {
        _entries.Clear();
        while (_errors.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _totalRenderCount, 0);
        Interlocked.Exchange(ref _totalRenderTicks, 0);
    }

    public void Dispose()
    {
        Reset();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Per-component diagnostic data.</summary>
public sealed class ComponentDiagnosticEntry
{
    public string ComponentId { get; }
    public int RenderCount { get; private set; }
    public long TotalRenderTicks { get; private set; }
    public int ErrorCount { get; private set; }
    public int ParameterChangeCount { get; private set; }
    public List<string> RecentParameterChanges { get; } = new();
    public DateTimeOffset FirstRenderTime { get; private set; }
    public DateTimeOffset LastRenderTime { get; private set; }

    public TimeSpan AverageRenderTime => RenderCount > 0
        ? TimeSpan.FromTicks(TotalRenderTicks / RenderCount)
        : TimeSpan.Zero;

    public ComponentDiagnosticEntry(string componentId)
    {
        ComponentId = componentId;
        FirstRenderTime = DateTimeOffset.UtcNow;
        LastRenderTime = FirstRenderTime;
    }

    public void RecordRender(long elapsedTicks)
    {
        RenderCount++;
        TotalRenderTicks += elapsedTicks;
        LastRenderTime = DateTimeOffset.UtcNow;
    }

    public void RecordParameterChange(string parameterName)
    {
        ParameterChangeCount++;
        RecentParameterChanges.Add(parameterName);
        if (RecentParameterChanges.Count > 20)
            RecentParameterChanges.RemoveAt(0);
    }

    public void RecordError() => ErrorCount++;
}

/// <summary>Record of a component error.</summary>
public sealed class ComponentErrorRecord
{
    public string ComponentId { get; }
    public Exception Exception { get; }
    public DateTimeOffset Timestamp { get; }

    public ComponentErrorRecord(string componentId, Exception exception, DateTimeOffset timestamp)
    {
        ComponentId = componentId;
        Exception = exception;
        Timestamp = timestamp;
    }
}
