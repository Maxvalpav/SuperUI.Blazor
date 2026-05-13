// SuperUI/Base/Services/SgCircuitAwareness.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Base.Services;

/// <summary>
/// Monitors Blazor Server circuit lifecycle events.
/// Provides awareness of circuit state (connected/disconnected).
/// Critical for Server-side resource cleanup and reconnection strategies.
/// </summary>
public class SgCircuitAwareness : CircuitHandler, IDisposable
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

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CurrentCircuitId = circuit.Id;
        _stateStore.Add(circuit.Id);
        _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
        CircuitOpened?.Invoke(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _stateStore.Remove(circuit.Id);
        _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
        CircuitClosed?.Invoke(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connection up: {CircuitId}", circuit.Id);
        ConnectionUp?.Invoke(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Connection down: {CircuitId}", circuit.Id);
        ConnectionDown?.Invoke(circuit.Id);
        return Task.CompletedTask;
    }

    /// <summary>Check if the current circuit is connected.</summary>
    public bool IsConnected(string? circuitId = null)
    {
        return _stateStore.IsActive(circuitId ?? CurrentCircuitId);
    }

    public void Dispose()
    {
        CircuitOpened = null;
        CircuitClosed = null;
        ConnectionUp = null;
        ConnectionDown = null;
        GC.SuppressFinalize(this);
    }
}

/// <summary>Interface for tracking circuit states.</summary>
public interface ISgCircuitStateStore
{
    void Add(string circuitId);

    void Remove(string circuitId);

    bool IsActive(string? circuitId);

    int ActiveCount { get; }
}

/// <summary>In-memory circuit state store (default).</summary>
internal sealed class InMemoryCircuitStateStore : ISgCircuitStateStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _circuits = new();

    public int ActiveCount => _circuits.Count;

    public void Add(string circuitId) => _circuits.TryAdd(circuitId, true);

    public void Remove(string circuitId) => _circuits.TryRemove(circuitId, out _);

    public bool IsActive(string? circuitId) =>
        circuitId != null && _circuits.ContainsKey(circuitId);
}
