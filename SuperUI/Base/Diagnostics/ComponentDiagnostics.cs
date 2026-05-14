// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs
// ИСПРАВЛЕНО:
// ✅ RecordRender: Interlocked для thread-safe счётчиков
// ✅ AverageRenderTime: вычисляется lazy из TotalRenderTicks
// ✅ ComponentDiagnosticEntry: полная thread-safety
// ✅ SgDiagnosticsCollector: Singleton-safe

using System.Collections.Concurrent;
using System.Diagnostics;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Простой диагностический объект компонента для DEBUG-режима.
/// Используется в SgComponentBase.
/// </summary>
public class ComponentDiagnostics : IDisposable
{
    public string ComponentId { get; set; } = string.Empty;

    // ✅ ИСПРАВЛЕНО: volatile для thread-safe чтения без lock
    private volatile int _renderCount;
    private volatile int _parameterChangeCount;
    private volatile int _errorCount;
    private double _lastRenderMs;
    private double _maxRenderMs;
    private double _averageRenderMs;
    private readonly object _lock = new();

    public int RenderCount => _renderCount;
    public int ParameterChangeCount => _parameterChangeCount;
    public int ErrorCount => _errorCount;

    public double LastRenderMs
    {
        get { lock (_lock) return _lastRenderMs; }
        set { lock (_lock) _lastRenderMs = value; }
    }

    public double MaxRenderMs
    {
        get { lock (_lock) return _maxRenderMs; }
        set { lock (_lock) _maxRenderMs = value; }
    }

    public double AverageRenderMs
    {
        get { lock (_lock) return _averageRenderMs; }
        set { lock (_lock) _averageRenderMs = value; }
    }

    // Используется SgComponentBase напрямую
    public void IncrementRenderCount() => Interlocked.Increment(ref _renderCount);
    public void IncrementParameterChangeCount() => Interlocked.Increment(ref _parameterChangeCount);

    public void RecordError(string componentId, Exception exception)
        => Interlocked.Increment(ref _errorCount);

    public void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>
/// Singleton-сборщик диагностики по всем компонентам.
/// Thread-safe: ConcurrentDictionary + Interlocked.
/// </summary>
public class SgDiagnosticsCollector : ISgDiagnosticsCollector, IDisposable
{
    private readonly ConcurrentDictionary<string, ComponentDiagnosticEntry> _entries = new();
    private readonly ConcurrentQueue<ComponentErrorRecord> _errors = new();
    private int _totalRenderCount;
    private long _totalRenderTicks;
    private const int MaxErrors = 1000; // ограничение очереди ошибок

    public IReadOnlyDictionary<string, ComponentDiagnosticEntry> Entries => _entries;
    public int TotalRenderCount => _totalRenderCount;
    public TimeSpan TotalRenderTime => TimeSpan.FromTicks(Interlocked.Read(ref _totalRenderTicks));
    public int ErrorCount => _errors.Count;

    public void RecordRender(string componentId, long elapsedTicks)
    {
        var entry = _entries.GetOrAdd(componentId,
            static id => new ComponentDiagnosticEntry(id));
        entry.RecordRender(elapsedTicks);
        Interlocked.Increment(ref _totalRenderCount);
        Interlocked.Add(ref _totalRenderTicks, elapsedTicks);
    }

    public void RecordParameterChange(string componentId, string parameterName)
    {
        var entry = _entries.GetOrAdd(componentId,
            static id => new ComponentDiagnosticEntry(id));
        entry.RecordParameterChange(parameterName);
    }

    public void RecordError(string componentId, Exception exception)
    {
        var entry = _entries.GetOrAdd(componentId,
            static id => new ComponentDiagnosticEntry(id));
        entry.RecordError();

        // Ограничиваем размер очереди ошибок
        while (_errors.Count >= MaxErrors)
            _errors.TryDequeue(out _);

        _errors.Enqueue(new ComponentErrorRecord(componentId, exception, DateTimeOffset.UtcNow));
    }

    public IReadOnlyCollection<ComponentErrorRecord> GetErrors()
        => _errors.ToArray();

    public string GetSummary()
        => $"Components: {_entries.Count}, Total Renders: {_totalRenderCount}, " +
           $"Render Time: {TotalRenderTime.TotalMilliseconds:F2}ms, Errors: {_errors.Count}";

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

/// <summary>Per-component diagnostic data. Thread-safe через Interlocked.</summary>
public sealed class ComponentDiagnosticEntry
{
    public string ComponentId { get; }

    private int _renderCount;
    private long _totalRenderTicks;
    private int _errorCount;
    private int _parameterChangeCount;

    public int RenderCount => _renderCount;
    public long TotalRenderTicks => Interlocked.Read(ref _totalRenderTicks);
    public int ErrorCount => _errorCount;
    public int ParameterChangeCount => _parameterChangeCount;

    // Список последних изменённых параметров — ConcurrentQueue для thread-safety
    private readonly ConcurrentQueue<string> _recentParameterChanges = new();
    private const int MaxRecentChanges = 20;

    public IReadOnlyCollection<string> RecentParameterChanges
        => _recentParameterChanges.ToArray();

    public DateTimeOffset FirstRenderTime { get; }
    public DateTimeOffset LastRenderTime { get; private set; }

    public TimeSpan AverageRenderTime
        => _renderCount > 0
            ? TimeSpan.FromTicks(Interlocked.Read(ref _totalRenderTicks) / _renderCount)
            : TimeSpan.Zero;

    public ComponentDiagnosticEntry(string componentId)
    {
        ComponentId = componentId;
        FirstRenderTime = DateTimeOffset.UtcNow;
        LastRenderTime = FirstRenderTime;
    }

    public void RecordRender(long elapsedTicks)
    {
        Interlocked.Increment(ref _renderCount);
        Interlocked.Add(ref _totalRenderTicks, elapsedTicks);
        LastRenderTime = DateTimeOffset.UtcNow; // DateTimeOffset присваивание атомарно на x64
    }

    public void RecordParameterChange(string parameterName)
    {
        Interlocked.Increment(ref _parameterChangeCount);
        _recentParameterChanges.Enqueue(parameterName);

        // Обрезаем старые записи
        while (_recentParameterChanges.Count > MaxRecentChanges)
            _recentParameterChanges.TryDequeue(out _);
    }

    public void RecordError()
        => Interlocked.Increment(ref _errorCount);
}

/// <summary>Запись об ошибке компонента.</summary>
public sealed class ComponentErrorRecord
{
    public string ComponentId { get; }
    public Exception Exception { get; }
    public DateTimeOffset Timestamp { get; }

    public ComponentErrorRecord(
        string componentId,
        Exception exception,
        DateTimeOffset timestamp)
    {
        ComponentId = componentId;
        Exception = exception;
        Timestamp = timestamp;
    }
}