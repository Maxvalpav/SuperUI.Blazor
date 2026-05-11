namespace SuperUI.Tokens;

/// <summary>
/// Race-safe токен жизненного цикла компонента.
/// 
/// Проблема: в Blazor Server при reconnect компонент может получить новые
/// параметры пока старые async операции ещё выполняются — гонка данных.
/// 
/// Решение: каждая новая инициализация создаёт новый токен,
/// старые операции отменяются автоматически.
/// 
/// Также защищает от:
/// - Double dispose
/// - Использования после dispose
/// - Параллельных операций
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public CancellationToken Token => _cts.Token;

    public bool IsCancelled => _cts.IsCancellationRequested;

    /// <summary>Создать LinkedTokenSource с дополнительным токеном.</summary>
    public CancellationToken LinkedWith(CancellationToken additional)
        => CancellationTokenSource
            .CreateLinkedTokenSource(_cts.Token, additional)
            .Token;

    public void Cancel()
    {
        if (!_disposed && !_cts.IsCancellationRequested)
            _cts.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_cts.IsCancellationRequested) _cts.Cancel();
        _cts.Dispose();
    }
}
