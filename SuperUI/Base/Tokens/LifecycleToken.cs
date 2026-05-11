// SuperUI/Base/Tokens/LifecycleToken.cs
// ИСПРАВЛЕНИЕ: защита от двойного Cancel(), thread-safe IsDisposed проверка
namespace SuperUI.Base.Tokens;

/// <summary>
/// CancellationTokenSource с lifecycle семантикой для Blazor компонента.
/// Создаётся при OnInitialized, отменяется при Dispose.
/// Thread-safe: Cancel() идемпотентен, Dispose() защищён от повторного вызова.
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private volatile int _disposed;

    public CancellationToken Token => _cts.Token;
    public bool IsCancelled => _cts.IsCancellationRequested;

    public void Cancel()
    {
        if (_disposed == 1) return;
        if (!_cts.IsCancellationRequested)
        {
            try { _cts.Cancel(); }
            catch (ObjectDisposedException) { /* already disposed */ }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _cts.Cancel(); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
