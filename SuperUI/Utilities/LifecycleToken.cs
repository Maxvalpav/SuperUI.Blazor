// Файл: Utilities/LifecycleToken.cs
// Зависимости: NONE
// ИННОВАЦИЯ: Решает проблему race condition при быстрой навигации

namespace SuperUI.Utilities;

/// <summary>
/// Race-safe токен жизненного цикла компонента.
/// Решает проблему: пользователь быстро переходит между страницами,
/// компонент A начинает async работу, но уже уничтожен когда она завершается.
/// 
/// ПАТТЕРН ИСПОЛЬЗОВАНИЯ:
/// var token = _lifecycleToken.Renew(); // при каждом init/parametersset
/// await SomeAsyncWork(token);
/// if (token.IsCancellationRequested) return; // компонент уже не нужен
/// 
/// ОТЛИЧИЕ ОТ ПРОСТОГО CTS:
/// - Автоматически отменяет предыдущий токен при Renew()
/// - Интегрируется с DisposeAsync
/// - Thread-safe через Interlocked
/// </summary>
public sealed class LifecycleToken : IAsyncDisposable
{
    private CancellationTokenSource _current = new();
    private int _disposed;

    /// <summary>
    /// Обновить токен (отменить предыдущий и создать новый).
    /// Вызывается в OnInitializedAsync и OnParametersSetAsync.
    /// </summary>
    public CancellationToken Renew()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
            return CancellationToken.None; // уже disposed, возвращаем уже отменённый

        var newCts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _current, newCts);
        old.Cancel();
        old.Dispose();
        return newCts.Token;
    }

    /// <summary>Текущий токен для передачи в async операции.</summary>
    public CancellationToken Current => _current.Token;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _current.CancelAsync();
        _current.Dispose();
    }
}
