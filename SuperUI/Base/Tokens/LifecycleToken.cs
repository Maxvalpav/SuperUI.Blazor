// SuperUI/Base/Tokens/LifecycleToken.cs
namespace SuperUI.Base.Tokens;

/// <summary>
/// Race-safe токен жизненного цикла компонента.
/// 
/// ИСПРАВЛЕНИЯ:
/// - LinkedWith: CancellationTokenSource теперь Dispose-безопасен через возврат структуры
/// - Double-dispose защита через Interlocked
/// - Thread-safe Cancel()
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private volatile int _disposed; // 0 = alive, 1 = disposed — Interlocked вместо bool

    public CancellationToken Token => _cts.Token;
    public bool IsCancelled => _cts.IsCancellationRequested;
    public bool IsDisposed => _disposed == 1;

    /// <summary>
    /// Создать связанный токен. ИСПРАВЛЕНО: возвращает LinkedTokenHandle для Dispose.
    /// </summary>
    public LinkedTokenHandle LinkedWith(CancellationToken additional)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, additional);
        return new LinkedTokenHandle(linked);
    }

    public void Cancel()
    {
        if (_disposed == 0 && !_cts.IsCancellationRequested)
        {
            try { _cts.Cancel(); }
            catch (ObjectDisposedException) { /* race в dispose — ок */ }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return; // double-dispose защита
        if (!_cts.IsCancellationRequested)
        {
            try { _cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }
        _cts.Dispose();
    }
}

/// <summary>
/// НОВЫЙ: Handle для linked CancellationTokenSource — гарантирует Dispose.
/// </summary>
public readonly struct LinkedTokenHandle : IDisposable
{
    private readonly CancellationTokenSource _cts;

    internal LinkedTokenHandle(CancellationTokenSource cts) => _cts = cts;

    public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

    public void Dispose() => _cts?.Dispose();
}
