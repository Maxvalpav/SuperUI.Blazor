// SuperUI/Base/Tokens/LifecycleToken.cs
namespace SuperUI.Base.Tokens;

/// <summary>
/// CancellationTokenSource с lifecycle-семантикой для Blazor компонента.
/// Создаётся при OnInitialized, отменяется при Dispose.
/// Thread-safe: Cancel идемпотентен.
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _disposed;

    public CancellationToken Token => _cts.Token;

    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void Cancel()
    {
        if (_disposed) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { /* already disposed — нормально */ }
    }

    public void Dispose()
    {
        _disposed = true;
        _cts.Dispose();
    }
}
