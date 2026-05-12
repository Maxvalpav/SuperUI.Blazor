// SuperUI/Base/Utilities/LifecycleToken.cs
// ✅ УЛУЧШЕНИЯ:
//   - CancelAsync() — async версия отмены (.NET 8+)
//   - IsDisposed свойство
//   - Reset() — пересоздание токена без пересоздания объекта

namespace SuperUI.Base.Utilities;

/// <summary>
/// CancellationTokenSource привязанный к жизненному циклу компонента.
/// Отменяется при Dispose или пересоздании компонента (навигация без unmount).
/// </summary>
internal sealed class LifecycleToken : IDisposable
{
    private CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Токен для использования в async операциях компонента.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>true если токен уже утилизирован.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true если отмена уже запрошена.</summary>
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>Синхронно отменить все операции.</summary>
    public void Cancel()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    /// <summary>Асинхронно отменить все операции (.NET 8+).</summary>
    public async Task CancelAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { await _cts.CancelAsync().ConfigureAwait(false); }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Отменить текущий токен и создать новый (для OnInitialized при повторной навигации).
    /// </summary>
    /// <returns>Новый активный токен.</returns>
    public CancellationToken Reset()
    {
        if (Volatile.Read(ref _disposed) == 1)
            return CancellationToken.None;

        var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        try { old.Cancel(); } catch { }
        try { old.Dispose(); } catch { }
        return _cts.Token;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _cts.Cancel(); } catch { }
        try { _cts.Dispose(); } catch { }
    }
}
