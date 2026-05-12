// SuperUI/Base/Utilities/LifecycleToken.cs
//
// НОВЫЙ: CancellationToken привязанный к жизненному циклу компонента.
// Отменяется при OnInitialized (переинициализация) и DisposeAsync.
// Используется для отмены JS interop вызовов при навигации/dispose.

namespace SuperUI.Base.Utilities;

/// <summary>
/// CancellationToken, привязанный к жизненному циклу компонента.
/// Создаётся в <c>OnInitialized</c>, отменяется при dispose или переинициализации.
/// </summary>
/// <remarks>
/// Thread safety: CancellationTokenSource — thread-safe.
/// WASM: работает корректно.
/// Server: каждый circuit — свой экземпляр (Scoped/per-component).
/// </remarks>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private int _disposed;

    public LifecycleToken()
    {
        _cts = new CancellationTokenSource();
    }

    /// <summary>CancellationToken жизненного цикла компонента.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Token отменён (компонент задиспожен или переинициализирован).</summary>
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>Запросить отмену (вызывается при OnInitialized/DisposeAsync).</summary>
    public void Cancel()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    /// <summary>Запросить отмену с задержкой.</summary>
    public void CancelAfter(TimeSpan delay)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _cts.CancelAfter(delay); }
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
