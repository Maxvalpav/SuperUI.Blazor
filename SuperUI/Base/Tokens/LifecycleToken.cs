namespace SuperUI.Base.Tokens;

/// <summary>
/// CancellationTokenSource с lifecycle семантикой для Blazor компонента.
/// Создаётся при OnInitialized, отменяется при Dispose.
///
/// ИСПРАВЛЕНО:
/// 1. Cancel() — Volatile.Read (ARM-safe, предотвращает устаревшее чтение _disposed).
/// 2. Dispose() — Interlocked.Exchange (идемпотентен, thread-safe).
/// 3. IsCancelled — удобное свойство.
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    public CancellationToken Token => _cts.Token;

    /// <summary>Удобный доступ — аналог Token.IsCancellationRequested.</summary>
    public bool IsCancelled => _cts.IsCancellationRequested;

    /// <summary>
    /// Отменить токен. Идемпотентен — безопасен для повторного вызова.
    /// Использует Volatile.Read для ARM-safety.
    /// </summary>
    public void Cancel()
    {
        // ИСПРАВЛЕНО: Volatile.Read вместо обычного чтения
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        // ИСПРАВЛЕНО: Interlocked.Exchange — атомарный compare-and-swap
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _cts.Cancel(); } catch { }
        _cts.Dispose();
    }
}
