// ================================================================
// Файл: SuperUI/Base/Services/SgCircuitAwareness.cs
// ИСПРАВЛЕНО:
// - Убрана прямая зависимость от CircuitHandler
// - Добавлен интерфейс ICircuitAwareness
// - Добавлена WasmCircuitAwareness заглушка
// ================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс для мониторинга состояния circuit (Server-side Blazor).
/// На WASM — заглушка WasmCircuitAwareness.
/// </summary>
public interface ICircuitAwareness
{
    event Action<string>? CircuitOpened;
    event Action<string>? CircuitClosed;
    event Action<string>? ConnectionUp;
    event Action<string>? ConnectionDown;

    int ActiveCircuitCount { get; }
    string? CurrentCircuitId { get; }
    bool IsConnected(string? circuitId = null);
}

/// <summary>
/// Server-side реализация circuit awareness.
/// CircuitHandler регистрируется отдельно через DI на Server.
/// </summary>
public sealed class SgCircuitAwareness : ICircuitAwareness, IDisposable
{
    private readonly ILogger _logger;
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

    /// <summary>
    /// Вызывается из адаптера CircuitHandler при открытии circuit.
    /// </summary>
    public void OnCircuitOpened(string circuitId)
    {
        CurrentCircuitId = circuitId;
        _stateStore.Add(circuitId);
        _logger.LogInformation("Circuit opened: {CircuitId}", circuitId);
        CircuitOpened?.Invoke(circuitId);
    }

    public void OnCircuitClosed(string circuitId)
    {
        _stateStore.Remove(circuitId);
        _logger.LogInformation("Circuit closed: {CircuitId}", circuitId);
        CircuitClosed?.Invoke(circuitId);
    }

    public void OnConnectionUp(string circuitId)
    {
        _logger.LogInformation("Connection up: {CircuitId}", circuitId);
        ConnectionUp?.Invoke(circuitId);
    }

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
/// WASM-заглушка — всегда "connected" (один "circuit").
/// </summary>
public sealed class WasmCircuitAwareness : ICircuitAwareness
{
    public static readonly WasmCircuitAwareness Instance = new();

    public event Action<string>? CircuitOpened { add { } remove { } }
    public event Action<string>? CircuitClosed { add { } remove { } }
    public event Action<string>? ConnectionUp { add { } remove { } }
    public event Action<string>? ConnectionDown { add { } remove { } }

    public int ActiveCircuitCount => 1;
    public string? CurrentCircuitId => "wasm-single";
    public bool IsConnected(string? circuitId = null) => true;
}

public interface ISgCircuitStateStore
{
    void Add(string circuitId);
    void Remove(string circuitId);
    bool IsActive(string? circuitId);
    int ActiveCount { get; }
}

internal sealed class InMemoryCircuitStateStore : ISgCircuitStateStore
{
    private readonly ConcurrentDictionary<string, bool> _circuits = new();

    public int ActiveCount => _circuits.Count;

    public void Add(string circuitId) => _circuits.TryAdd(circuitId, true);

    public void Remove(string circuitId) => _circuits.TryRemove(circuitId, out _);

    public bool IsActive(string? circuitId)
        => circuitId != null && _circuits.ContainsKey(circuitId);
}
