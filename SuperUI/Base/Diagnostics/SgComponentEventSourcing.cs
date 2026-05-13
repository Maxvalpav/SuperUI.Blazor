// SuperUI/Base/Diagnostics/SgComponentEventSourcing.cs
// 🆕 Event Sourcing для компонентов Blazor.
// Записывает все изменения состояния как события.
// Возможность replay для отладки.
// Ни у кого нет.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Base class for component events.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(ParameterChangedEvent), typeDiscriminator: "param")]
[JsonDerivedType(typeof(RenderEvent), typeDiscriminator: "render")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
[JsonDerivedType(typeof(StateChangedEvent), typeDiscriminator: "state")]
[JsonDerivedType(typeof(UserInteractionEvent), typeDiscriminator: "user")]
[JsonDerivedType(typeof(LifecycleEvent), typeDiscriminator: "lifecycle")]
[JsonDerivedType(typeof(JsInteropEvent), typeDiscriminator: "js")]
[JsonDerivedType(typeof(SignalChangedEvent), typeDiscriminator: "signal")]
public abstract record ComponentEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string ComponentId { get; init; } = null!;
    public string ComponentType { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public long SequenceNumber { get; init; }
    public string? CorrelationId { get; init; }
    public Dictionary<string, string>? Tags { get; init; }
}

public sealed record ParameterChangedEvent : ComponentEvent
{
    public string ParameterName { get; init; } = null!;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

public sealed record RenderEvent : ComponentEvent
{
    public bool IsFirstRender { get; init; }
    public double RenderTimeMs { get; init; }
    public int RenderCount { get; init; }
}

public sealed record ErrorEvent : ComponentEvent
{
    public string ErrorMessage { get; init; } = null!;
    public string? StackTrace { get; init; }
    public string? ErrorType { get; init; }
}

public sealed record StateChangedEvent : ComponentEvent
{
    public string StateKey { get; init; } = null!;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

public sealed record UserInteractionEvent : ComponentEvent
{
    public string InteractionType { get; init; } = null!; // click, keypress, focus, etc.
    public string? Key { get; init; }
    public string? TargetElement { get; init; }
}

public sealed record LifecycleEvent : ComponentEvent
{
    public string Phase { get; init; } = null!; // OnInitialized, OnParametersSet, OnAfterRender, etc.
    public double DurationMs { get; init; }
}

public sealed record JsInteropEvent : ComponentEvent
{
    public string FunctionIdentifier { get; init; } = null!;
    public double DurationMs { get; init; }
    public bool IsVoid { get; init; }
    public string? Result { get; init; }
}

public sealed record SignalChangedEvent : ComponentEvent
{
    public string SignalName { get; init; } = null!;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

/// <summary>
/// Event store for component event sourcing.
/// Stores events in a circular buffer per component.
/// Supports replay, export, and querying.
/// </summary>
public sealed class SgComponentEventStore : IDisposable
{
    private readonly ConcurrentDictionary<string, CircularEventBuffer> _stores = new();
    private readonly int _maxEventsPerComponent;
    private readonly int _maxTotalEvents;
    private long _totalEvents;

    public long TotalEvents => Interlocked.Read(ref _totalEvents);

    public SgComponentEventStore(int maxEventsPerComponent = 1000, int maxTotalEvents = 1_000_000)
    {
        _maxEventsPerComponent = maxEventsPerComponent;
        _maxTotalEvents = maxTotalEvents;
    }

    /// <summary>
    /// Record an event.
    /// </summary>
    public void Record(ComponentEvent evt)
    {
        if (Interlocked.Read(ref _totalEvents) >= _maxTotalEvents)
            return; // Drop events when full

        var buffer = _stores.GetOrAdd(evt.ComponentId,
            _ => new CircularEventBuffer(_maxEventsPerComponent));
        buffer.Add(evt);
        Interlocked.Increment(ref _totalEvents);
    }

    /// <summary>
    /// Get all events for a component.
    /// </summary>
    public IReadOnlyList<ComponentEvent> GetEvents(string componentId)
    {
        return _stores.TryGetValue(componentId, out var buffer)
            ? buffer.ToList()
            : Array.Empty<ComponentEvent>();
    }

    /// <summary>
    /// Get events of a specific type for a component.
    /// </summary>
    public IReadOnlyList<T> GetEvents<T>(string componentId) where T : ComponentEvent
    {
        return _stores.TryGetValue(componentId, out var buffer)
            ? buffer.ToList().OfType<T>().ToList()
            : Array.Empty<T>();
    }

    /// <summary>
    /// Replay events for a component — invoke callback for each event in sequence.
    /// </summary>
    public async Task ReplayAsync(string componentId,
        Func<ComponentEvent, Task> handler,
        CancellationToken ct = default)
    {
        if (!_stores.TryGetValue(componentId, out var buffer))
            return;

        foreach (var evt in buffer.ToList())
        {
            if (ct.IsCancellationRequested) break;
            await handler(evt);
        }
    }

    /// <summary>
    /// Export all events as JSON.
    /// </summary>
    public string ExportJson(string? componentId = null)
    {
        var events = componentId != null
            ? GetEvents(componentId)
            : _stores.Values.SelectMany(b => b.ToList()).ToList();

        return JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Clear events for a component.
    /// </summary>
    public void Clear(string componentId)
    {
        _stores.TryRemove(componentId, out _);
    }

    /// <summary>
    /// Clear all events.
    /// </summary>
    public void ClearAll()
    {
        _stores.Clear();
        Interlocked.Exchange(ref _totalEvents, 0);
    }

    public void Dispose()
    {
        ClearAll();
    }

    private sealed class CircularEventBuffer
    {
        private readonly ComponentEvent[] _buffer;
        private int _head;
        private int _count;
        private readonly object _lock = new();

        public CircularEventBuffer(int capacity)
        {
            _buffer = new ComponentEvent[capacity];
        }

        public void Add(ComponentEvent evt)
        {
            lock (_lock)
            {
                _buffer[_head] = evt;
                _head = (_head + 1) % _buffer.Length;
                if (_count < _buffer.Length)
                    _count++;
            }
        }

        public List<ComponentEvent> ToList()
        {
            lock (_lock)
            {
                var result = new List<ComponentEvent>(_count);
                var start = _count < _buffer.Length ? 0 : _head;

                for (var i = 0; i < _count; i++)
                {
                    result.Add(_buffer[(start + i) % _buffer.Length]);
                }

                return result;
            }
        }
    }
}
