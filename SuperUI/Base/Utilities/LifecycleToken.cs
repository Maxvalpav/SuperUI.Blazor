// SuperUI/Base/Utilities/LifecycleToken.cs

namespace SuperUI.Base.Utilities;

/// <summary>
/// CancellationTokenSource привязанный к жизненному циклу компонента.
/// Отменяется при Dispose или пересоздании компонента (навигация без unmount).
/// </summary>
internal sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Токен для использования в async операциях компонента.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Отменить все операции привязанные к этому токену.</summary>
    public void Cancel()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _cts.Cancel(); }
        catch { }
        try { _cts.Dispose(); }
        catch { }
    }
}
