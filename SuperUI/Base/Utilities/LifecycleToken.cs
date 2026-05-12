// SuperUI/Base/Utilities/LifecycleToken.cs
//
// Отменяемый CancellationToken привязанный к жизненному циклу компонента.
// Используется в SgJsComponentBase для отмены JS-вызовов при Dispose.
//
// Thread-safe: CancellationTokenSource внутри потокобезопасен.

namespace SuperUI.Base.Utilities;

/// <summary>
/// Управляемый токен отмены для жизненного цикла компонента.
/// Отменяется при Dispose или при явном вызове Cancel().
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Токен отмены компонента.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Отменить все ожидающие операции.</summary>
    public void Cancel()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    /// <summary>Сбросить токен (создать новый CTS).</summary>
    public void Reset()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        try
        {
            old.Cancel();
            old.Dispose();
        }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch (ObjectDisposedException) { }
    }
}
