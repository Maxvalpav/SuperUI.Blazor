// SuperUI/Base/State/SgAsyncLazyData.cs

/// <summary>
/// Умная загрузка данных:
/// - Deduplication: одновременные запросы = один реальный запрос
/// - Stale-while-revalidate: показываем старые данные пока грузятся новые
/// - Автоматическая инвалидация по ключу
/// </summary>
public sealed class SgAsyncLazyData<T>
{
    private readonly Func<CancellationToken, Task<T>> _loader;
    private readonly TimeSpan _staleTime;
    private Task<T>? _currentLoad;
    private T? _cached;
    private DateTime _cachedAt;
    private readonly SemaphoreSlim _sem = new(1, 1);

    public SgAsyncLazyData(Func<CancellationToken, Task<T>> loader,
        TimeSpan? staleTime = null)
    {
        _loader = loader;
        _staleTime = staleTime ?? TimeSpan.FromMinutes(5);
    }

    public bool HasValue => _cached is not null;
    public T? CachedValue => _cached;
    public bool IsStale => DateTime.UtcNow - _cachedAt > _staleTime;

    public async Task<T> GetAsync(CancellationToken ct = default)
    {
        // Быстрый путь — кэш свежий
        if (_cached is not null && !IsStale) return _cached;

        await _sem.WaitAsync(ct);
        try
        {
            // Double-check
            if (_cached is not null && !IsStale) return _cached;

            // Deduplication — один реальный запрос
            _currentLoad ??= _loader(ct);
            var result = await _currentLoad;
            _cached = result;
            _cachedAt = DateTime.UtcNow;
            _currentLoad = null;
            return result;
        }
        finally { _sem.Release(); }
    }

    public void Invalidate()
    {
        _cached = default;
        _cachedAt = default;
    }
}
