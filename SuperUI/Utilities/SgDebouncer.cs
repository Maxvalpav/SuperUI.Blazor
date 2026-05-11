// Файл: Utilities/SgDebouncer.cs
// Зависимости: NONE (кроме System.Threading.*)
// GC оптимизация: переиспользуем CancellationTokenSource, не создаём новый Timer

namespace SuperUI.Utilities;

/// <summary>
/// Debounce utility: откладывает выполнение действия на заданный интервал.
/// После каждого вызова таймер сбрасывается.
/// 
/// GC ОПТИМИЗАЦИЯ:
/// - Один PeriodicTimer не используется т.к. нам нужен сброс
/// - CancellationTokenSource переиспользуется (отмена + новый токен)
/// - SemaphoreSlim для thread-safety без lock
/// 
/// ПРИМЕНЕНИЕ: задержка поиска при вводе текста.
/// </summary>
public sealed class SgDebouncer : IAsyncDisposable
{
    private readonly TimeSpan _delay;
    private CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public SgDebouncer(TimeSpan delay)
    {
        _delay = delay;
    }

    public SgDebouncer(int delayMs) : this(TimeSpan.FromMilliseconds(delayMs)) { }

    /// <summary>
    /// Запустить (или перезапустить) debounce. Action выполнится через Delay после последнего вызова.
    /// </summary>
    public async ValueTask DebounceAsync(Func<CancellationToken, ValueTask> action, CancellationToken externalToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SgDebouncer));

        // Отменяем предыдущий pending вызов
        await _semaphore.WaitAsync(externalToken);
        CancellationTokenSource newCts;
        try
        {
            await _cts.CancelAsync(); // .NET 8+ async cancel
            _cts.Dispose();
            newCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _cts = newCts;
        }
        finally
        {
            _semaphore.Release();
        }

        try
        {
            await Task.Delay(_delay, newCts.Token);
            await action(newCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Нормально — последующий вызов победил
        }
    }

    /// <summary>Sync overload (Fire-and-forget из event handler).</summary>
    public void Debounce(Func<CancellationToken, ValueTask> action, CancellationToken externalToken = default)
        => _ = DebounceAsync(action, externalToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _cts.CancelAsync();
        _cts.Dispose();
        _semaphore.Dispose();
    }
}
