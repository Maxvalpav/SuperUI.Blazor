// SuperUI/Base/Tokens/LifecycleToken.cs
// ИСПРАВЛЕНО:
// 1. Убрана излишняя проверка IsCancellationRequested в Cancel() — CTS.Cancel() идемпотентен
// 2. volatile int _disposed (убран volatile — Interlocked достаточен)
// 3. Добавлен IsCancelled property для удобства
namespace SuperUI.Base.Tokens;

/// <summary>
/// CancellationTokenSource с lifecycle семантикой для Blazor компонента.
/// Создаётся при OnInitialized, отменяется при Dispose.
/// Thread-safe: Cancel() идемпотентен, Dispose() защищён от повторного вызова.
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    // ИСПРАВЛЕНО: убран volatile — Interlocked.Exchange обеспечивает необходимые memory barriers
    private int _disposed;

    public CancellationToken Token => _cts.Token;

    /// <summary>Удобный доступ — аналог Token.IsCancellationRequested</summary>
    public bool IsCancelled => _cts.IsCancellationRequested;

    /// <summary>
    /// Отменить токен. Идемпотентен — безопасен для повторного вызова.
    /// </summary>
    public void Cancel()
    {
        if (_disposed == 1) return;
        try
        {
            // ИСПРАВЛЕНО: CancellationTokenSource.Cancel() является no-op при повторном вызове.
            // Убрана проверка IsCancellationRequested — она создавала TOCTOU race window.
            _cts.Cancel();
        }
        catch (ObjectDisposedException) { /* уже задиспожен — нормально */ }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _cts.Cancel(); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
