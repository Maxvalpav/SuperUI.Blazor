// SuperUI/Base/Diagnostics/SgSignalDevTools.cs — НОВЫЙ КЛАСС
// Аналог: Vue DevTools, Zustand DevTools, Redux DevTools
// Поддержка: .NET 8/9/10, только InteractiveServer + WASM (не SSR)
// Регистрация: builder.Services.AddScoped<SgSignalDevTools>() (только в DEBUG)

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// DevTools для инспекции сигналов SuperUI.
/// В production-режиме — noop (все методы пустые).
/// <para>
/// Использование в компоненте:
/// <code>
/// #if DEBUG
/// [Inject] SgSignalDevTools DevTools { get; set; } = null!;
///
/// protected override void OnInitialized()
/// {
///     DevTools.Track(mySignal, "MyComponent.count");
/// }
/// #endif
/// </code>
/// </para>
/// </summary>
public sealed class SgSignalDevTools : IDisposable
{
    private readonly ILogger<SgSignalDevTools>? _logger;
    private readonly ConcurrentDictionary<string, SignalSnapshot> _snapshots = new();
    private readonly List<SignalEvent> _eventLog = [];
    private readonly object _logLock = new();
    private int _disposed;

    // Максимальный размер лога событий (предотвращает утечку памяти)
    private const int MaxEventLogSize = 1000;

    public SgSignalDevTools(ILogger<SgSignalDevTools>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Начать отслеживание сигнала.</summary>
    [Conditional("DEBUG")]
    public void Track<T>(SgSignal<T> signal, string? label = null)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var name = label ?? signal.DebugName ?? typeof(T).Name;
        var observer = new DevToolsObserver<T>(name, this);
        signal.Subscribe(observer);

        _snapshots[name] = new SignalSnapshot(
            name,
            SerializeValue(signal.Value),
            DateTimeOffset.UtcNow);

        LogEvent(new SignalEvent(name, "init", SerializeValue(signal.Value)));
    }

    /// <summary>Получить текущие снимки всех отслеживаемых сигналов.</summary>
    public IReadOnlyDictionary<string, SignalSnapshot> GetSnapshots()
        => _snapshots;

    /// <summary>Получить лог событий.</summary>
    public IReadOnlyList<SignalEvent> GetEventLog()
    {
        lock (_logLock) return _eventLog.ToList().AsReadOnly();
    }

    /// <summary>Очистить лог событий.</summary>
    public void ClearLog()
    {
        lock (_logLock) _eventLog.Clear();
    }

    internal void OnSignalChanged<T>(string name, T value)
    {
        var json = SerializeValue(value);
        _snapshots[name] = new SignalSnapshot(name, json, DateTimeOffset.UtcNow);
        LogEvent(new SignalEvent(name, "change", json));
        _logger?.LogDebug("[SignalDevTools] {Name} = {Value}", name, json);
    }

    private void LogEvent(SignalEvent evt)
    {
        lock (_logLock)
        {
            if (_eventLog.Count >= MaxEventLogSize)
                _eventLog.RemoveAt(0);
            _eventLog.Add(evt);
        }
    }

    private static string SerializeValue<T>(T value)
    {
        try { return JsonSerializer.Serialize(value); }
        catch { return value?.ToString() ?? "null"; }
    }

    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

    private sealed class DevToolsObserver<T> : ISignalObserver<T>
    {
        private readonly string _name;
        private readonly SgSignalDevTools _devTools;

        public DevToolsObserver(string name, SgSignalDevTools devTools)
        {
            _name = name;
            _devTools = devTools;
        }

        public void OnSignalChanged(ISgSignal<T> signal)
            => _devTools.OnSignalChanged(_name, signal.Value);
    }
}

/// <summary>Снимок значения сигнала в момент времени.</summary>
public sealed record SignalSnapshot(string Name, string JsonValue, DateTimeOffset CapturedAt);

/// <summary>Событие изменения сигнала.</summary>
public sealed record SignalEvent(string SignalName, string EventType, string JsonValue)
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
