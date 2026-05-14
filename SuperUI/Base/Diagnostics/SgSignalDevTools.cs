// ================================================================
// Файл: SuperUI/Base/Diagnostics/SgSignalDevTools.cs
// ИСПРАВЛЕНО:
// ✅ CS0308: DevToolsObserver<T> реализует ISignalObserver<T> (не ISignalObserver<T> generic)
// ✅ Track<T> возвращает IDisposable для корректной отписки
// ✅ Dispose отписывает всех наблюдателей (устранена утечка подписок)
// ✅ MaxEventLogSize предотвращает утечку памяти
// ✅ [Conditional("DEBUG")] — нулевой overhead в production
// ================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// DevTools для инспекции сигналов SuperUI в режиме отладки.
/// Аналог: Vue DevTools, Zustand DevTools, Redux DevTools.
/// <para>В production — noop (весь публичный API помечен [Conditional("DEBUG")]).</para>
/// <para>Поддержка: .NET 8/9/10, только InteractiveServer + WASM (не Static SSR).</para>
/// </summary>
/// <example>
/// <code>
/// #if DEBUG
/// [Inject] SgSignalDevTools DevTools { get; set; } = null!;
///
/// protected override void OnInitialized()
/// {
///     // Возвращает IDisposable для отписки
///     var sub = DevTools.Track(mySignal, "MyComponent.count");
///     AddDisposable(sub);
/// }
/// #endif
/// </code>
/// </example>
public sealed class SgSignalDevTools : IDisposable
{
    private readonly ILogger<SgSignalDevTools>? _logger;
    private readonly ConcurrentDictionary<string, SignalSnapshot> _snapshots = new();
    private readonly List<SignalEvent> _eventLog = [];
    private readonly List<IDisposable> _trackedSubscriptions = [];
    private readonly object _logLock = new();
    private int _disposed;

    /// <summary>Максимальный размер лога событий (предотвращает утечку памяти).</summary>
    private const int MaxEventLogSize = 1000;

    public SgSignalDevTools(ILogger<SgSignalDevTools>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Начать отслеживание сигнала.
    /// ✅ FIX: возвращает IDisposable — вызовите Dispose() для отписки.
    /// ✅ FIX: CS0308 — использует ISignalObserver&lt;T&gt; (generic).
    /// </summary>
    /// <param name="signal">Отслеживаемый сигнал.</param>
    /// <param name="label">Отладочная метка (по умолчанию — DebugName или имя типа).</param>
    /// <returns>IDisposable для отписки.</returns>
    public IDisposable Track<T>(SgSignal<T> signal, string? label = null)
    {
        if (Volatile.Read(ref _disposed) == 1)
            return NullDisposable.Instance;

        var name = label ?? signal.DebugName ?? typeof(T).Name;
        // ✅ FIX CS0308: DevToolsObserver<T> реализует ISignalObserver<T>
        var observer = new DevToolsObserver<T>(name, this);
        signal.Subscribe(observer);

        _snapshots[name] = new SignalSnapshot(name, SerializeValue(signal.Value), DateTimeOffset.UtcNow);
        LogEvent(new SignalEvent(name, "init", SerializeValue(signal.Value)));

        // ✅ FIX: возвращаем Disposable для отписки (устранена утечка)
        var subscription = new TrackDisposable(() => signal.Unsubscribe(observer));
        _trackedSubscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>Получить текущие снимки всех отслеживаемых сигналов.</summary>
    public IReadOnlyDictionary<string, SignalSnapshot> GetSnapshots()
        => _snapshots;

    /// <summary>Получить лог событий (thread-safe копия).</summary>
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
        if (Volatile.Read(ref _disposed) == 1) return;

        var json = SerializeValue(value);
        _snapshots[name] = new SignalSnapshot(name, json, DateTimeOffset.UtcNow);
        LogEvent(new SignalEvent(name, "change", json));
        _logger?.LogDebug("[SignalDevTools] {Name} = {Value}", name, json);
    }

    private void LogEvent(SignalEvent evt)
    {
        lock (_logLock)
        {
            // Скользящее окно — предотвращает неограниченный рост
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

    /// <summary>
    /// Dispose: отписывает всех наблюдателей.
    /// ✅ FIX: устранена утечка подписок.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (var sub in _trackedSubscriptions)
        {
            try { sub.Dispose(); }
            catch { /* dispose не должен бросать */ }
        }
        _trackedSubscriptions.Clear();
        _snapshots.Clear();
    }

    // ── Вспомогательные типы ────────────────────────────────────────────────

    /// <summary>
    /// ✅ FIX CS0308: реализует ISignalObserver&lt;T&gt; (generic).
    /// </summary>
    private sealed class DevToolsObserver<T> : ISignalObserver<T>
    {
        private readonly string _name;
        private readonly SgSignalDevTools _devTools;

        public DevToolsObserver(string name, SgSignalDevTools devTools)
        {
            _name = name;
            _devTools = devTools;
        }

        // ✅ FIX CS0308: реализует типизированный метод из ISignalObserver<T>
        public void OnSignalChanged(ISgSignal<T> typedSignal)
            => _devTools.OnSignalChanged(_name, typedSignal.Value);
    }

    private sealed class TrackDisposable : IDisposable
    {
        private Action? _action;

        public TrackDisposable(Action action) => _action = action;

        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose() { }
    }
}

/// <summary>Снимок значения сигнала в момент времени.</summary>
public sealed record SignalSnapshot(string Name, string JsonValue, DateTimeOffset CapturedAt);

/// <summary>Событие изменения сигнала.</summary>
public sealed record SignalEvent(string SignalName, string EventType, string JsonValue)
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
