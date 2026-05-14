// ================================================================
// Файл: SuperUI/Base/Services/SgCircuitAwareness.cs
// ИСПРАВЛЕНО:
// ✅ CS0246/CS0311: ISgCircuitAwareness — добавлен как alias для ICircuitAwareness
// ✅ ServiceCollectionExtensions может использовать оба имени
// ✅ WasmCircuitAwareness — полная заглушка
// ✅ ISgCircuitStateStore — потокобезопасный InMemory вариант
// УЛУЧШЕНО:
// ✅ SgCircuitAwareness: опциональный logger через NullLogger
// ✅ Thread-safe: ConcurrentDictionary
// ================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Base.Services;

/// <summary>
/// Основной интерфейс для мониторинга состояния Blazor Server circuit.
/// На WASM реализуется заглушкой WasmCircuitAwareness.
/// </summary>
public interface ICircuitAwareness
{
    /// <summary>Вызывается при открытии нового circuit.</summary>
    event Action<string>? CircuitOpened;

    /// <summary>Вызывается при закрытии circuit (disconnect/timeout).</summary>
    event Action<string>? CircuitClosed;

    /// <summary>Вызывается при восстановлении соединения.</summary>
    event Action<string>? ConnectionUp;

    /// <summary>Вызывается при потере соединения.</summary>
    event Action<string>? ConnectionDown;

    /// <summary>Количество активных circuits (Server-side).</summary>
    int ActiveCircuitCount { get; }

    /// <summary>ID текущего circuit (null на WASM).</summary>
    string? CurrentCircuitId { get; }

    /// <summary>Проверить активность circuit по ID.</summary>
    bool IsConnected(string? circuitId = null);
}

/// <summary>
/// ✅ FIX CS0246: псевдоним для обратной совместимости.
/// ServiceCollectionExtensions может использовать ISgCircuitAwareness вместо ICircuitAwareness.
/// </summary>
public interface ISgCircuitAwareness : ICircuitAwareness { }

/// <summary>
/// Server-side реализация circuit awareness.
/// Вызывается из CircuitHandler-адаптера (регистрируется отдельно через DI).
/// </summary>
public sealed class SgCircuitAwareness : ISgCircuitAwareness, IDisposable
{
    private readonly ILogger<SgCircuitAwareness> _logger;
    private readonly ISgCircuitStateStore _stateStore;

    public event Action<string>? CircuitOpened;
    public event Action<string>? CircuitClosed;
    public event Action<string>? ConnectionUp;
    public event Action<string>? ConnectionDown;

    public int ActiveCircuitCount => _stateStore.ActiveCount;
    public string? CurrentCircuitId { get; private set; }

    public SgCircuitAwareness(ILogger<SgCircuitAwareness>? logger = null,
        ISgCircuitStateStore? stateStore = null)
    {
        _logger = logger ?? NullLogger<SgCircuitAwareness>.Instance;
        _stateStore = stateStore ?? new InMemoryCircuitStateStore();
    }

    /// <summary>Вызывается при открытии нового circuit (из CircuitHandler).</summary>
    public void OnCircuitOpened(string circuitId)
    {
        CurrentCircuitId = circuitId;
        _stateStore.Add(circuitId);
        _logger.LogInformation("Circuit opened: {CircuitId}", circuitId);
        CircuitOpened?.Invoke(circuitId);
    }

    /// <summary>Вызывается при закрытии circuit (из CircuitHandler).</summary>
    public void OnCircuitClosed(string circuitId)
    {
        _stateStore.Remove(circuitId);
        if (string.Equals(CurrentCircuitId, circuitId, StringComparison.Ordinal))
            CurrentCircuitId = null;
        _logger.LogInformation("Circuit closed: {CircuitId}", circuitId);
        CircuitClosed?.Invoke(circuitId);
    }

    /// <summary>Вызывается при восстановлении соединения.</summary>
    public void OnConnectionUp(string circuitId)
    {
        _logger.LogInformation("Connection up: {CircuitId}", circuitId);
        ConnectionUp?.Invoke(circuitId);
    }

    /// <summary>Вызывается при потере соединения.</summary>
    public void OnConnectionDown(string circuitId)
    {
        _logger.LogWarning("Connection down: {CircuitId}", circuitId);
        ConnectionDown?.Invoke(circuitId);
    }

    public bool IsConnected(string? circuitId = null)
        => _stateStore.IsActive(circuitId ?? CurrentCircuitId);

    public void Dispose()
    {
        CircuitOpened = null;
        CircuitClosed = null;
        ConnectionUp = null;
        ConnectionDown = null;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// WASM-заглушка circuit awareness.
/// В WASM всегда "connected" — нет понятия circuit.
/// </summary>
public sealed class WasmCircuitAwareness : ISgCircuitAwareness
{
    public static readonly WasmCircuitAwareness Instance = new();

    // События — noop (нет subscribers в WASM)
    public event Action<string>? CircuitOpened { add { } remove { } }
    public event Action<string>? CircuitClosed { add { } remove { } }
    public event Action<string>? ConnectionUp   { add { } remove { } }
    public event Action<string>? ConnectionDown { add { } remove { } }

    public int ActiveCircuitCount => 1;
    public string? CurrentCircuitId => "wasm-single";
    public bool IsConnected(string? circuitId = null) => true;
}

// ── Circuit State Store ────────────────────────────────────────────────────

/// <summary>Интерфейс хранилища состояний circuit.</summary>
public interface ISgCircuitStateStore
{
    void Add(string circuitId);
    void Remove(string circuitId);
    bool IsActive(string? circuitId);
    int ActiveCount { get; }
}

/// <summary>In-memory thread-safe хранилище circuit ID.</summary>
internal sealed class InMemoryCircuitStateStore : ISgCircuitStateStore
{
    private readonly ConcurrentDictionary<string, bool> _circuits = new();

    public int ActiveCount => _circuits.Count;

    public void Add(string circuitId) => _circuits.TryAdd(circuitId, true);

    public void Remove(string circuitId) => _circuits.TryRemove(circuitId, out _);

    public bool IsActive(string? circuitId) => circuitId != null && _circuits.ContainsKey(circuitId);
}
