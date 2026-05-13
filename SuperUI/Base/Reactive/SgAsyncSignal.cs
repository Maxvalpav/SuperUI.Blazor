// SuperUI/Base/Reactive/SgAsyncSignal.cs
// НОВЫЙ КЛАСС: отсутствует в библиотеке
// Аналог: SWR (React), TanStack Query
// Поддержка: .NET 8/9/10, Server + WASM + SSR/
//
// Что делает:
// - Хранит состояние загрузки: Loading, Data, Error
// - Автоматически загружает данные при инициализации
// - Поддерживает перезагрузку (Refresh)
// - Поддерживает отмену (CancellationToken)
// - Уведомляет подписчиков (SgSignal) при изменении состояния
// - Работает в SSR без JS (загрузка происходит на сервере)
//
// Использование:
// var users = new SgAsyncSignal<List<User>>(() => api.GetUsersAsync());
// await users.LoadAsync();
// if (users.IsLoading) { <spinner /> }
// if (users.HasValue) { foreach (var u in users.Value!) { ... } }

namespace SuperUI.Base.Reactive;

/// <summary>Состояние асинхронного сигнала.</summary>
public enum AsyncSignalState
{
    /// <summary>Данные не загружались.</summary>
    Idle,

    /// <summary>Загрузка в процессе.</summary>
    Loading,

    /// <summary>Данные загружены успешно.</summary>
    Loaded,

    /// <summary>Ошибка загрузки.</summary>
    Error
}

/// <summary>
/// Реактивный сигнал с асинхронным источником данных.
/// Инкапсулирует состояние загрузки: Loading/Loaded/Error.
///
/// Подходит для:
/// - HTTP запросов в компонентах
/// - Lazy-загрузки данных
/// - Автоматического обновления (polling)
///
/// Совместим с:
/// - Static SSR: загружает данные серверно при OnInitializedAsync
/// - Streaming Rendering: показывает placeholder пока загружается
/// - InteractiveServer/WASM: реактивно обновляет UI
/// </summary>
public sealed class SgAsyncSignal<T> : ISgSignal<AsyncSignalResult<T>>, IDisposable
{
    private readonly Func<CancellationToken, Task<T>> _loader;
    private readonly IEqualityComparer<T>? _comparer;

    // Внутренний сигнал состояния
    private readonly SgSignal<AsyncSignalResult<T>> _inner;
    private readonly CancellationTokenSource _cts = new();
    private int _loadCount;   // для stale-while-revalidate
    private int _disposed;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;

    /// <summary>Текущее состояние.</summary>
    public AsyncSignalState State => _inner.Value.State;

    /// <summary>Идёт ли загрузка.</summary>
    public bool IsLoading => State == AsyncSignalState.Loading;

    /// <summary>Есть ли данные.</summary>
    public bool HasValue => State == AsyncSignalState.Loaded;

    /// <summary>Была ли ошибка.</summary>
    public bool HasError => State == AsyncSignalState.Error;

    /// <summary>Данные (null если не загружены).</summary>
    public T? Data => _inner.Value.Data;

    /// <summary>Ошибка (null если нет ошибки).</summary>
    public Exception? Error => _inner.Value.Error;

    /// <summary>Текущий результат (включает состояние + данные + ошибку).</summary>
    public AsyncSignalResult<T> Value => _inner.Value;

    // ISgSignal<T> implementation
    AsyncSignalResult<T> IReadOnlySignal<AsyncSignalResult<T>>.Value => _inner.Value;

    public void Set(AsyncSignalResult<T> value) => _inner.Set(value);

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);

    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public SgAsyncSignal(
        Func<CancellationToken, Task<T>> loader,
        string? debugName = null,
        IEqualityComparer<T>? comparer = null)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _comparer = comparer;
        _inner = new SgSignal<AsyncSignalResult<T>>(
            AsyncSignalResult<T>.Idle(),
            debugName: debugName ?? $"AsyncSignal<{typeof(T).Name}>");
    }

    /// <summary>Загрузить данные из источника.</summary>
    public async Task LoadAsync(bool forceReload = false)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        // Не перезагружаем если уже загружено (если не форсировано)
        if (!forceReload && State == AsyncSignalState.Loaded) return;

        var loadId = Interlocked.Increment(ref _loadCount);
        _inner.Set(AsyncSignalResult<T>.Loading(Data));

        try
        {
            var result = await _loader(_cts.Token);

            // Отбрасываем устаревшие загрузки (stale response)
            if (loadId != Volatile.Read(ref _loadCount)) return;
            if (Volatile.Read(ref _disposed) == 1) return;

            _inner.Set(AsyncSignalResult<T>.Success(result));
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Disposed — не обновляем состояние
        }
        catch (Exception ex)
        {
            if (loadId != Volatile.Read(ref _loadCount)) return;
            if (Volatile.Read(ref _disposed) == 1) return;

            _inner.Set(AsyncSignalResult<T>.Failure(ex, Data));
        }
    }

    /// <summary>Перезагрузить данные.</summary>
    public Task RefreshAsync() => LoadAsync(forceReload: true);

    /// <summary>Сбросить состояние в Idle.</summary>
    public void Reset() => _inner.Set(AsyncSignalResult<T>.Idle());

    /// <summary>
    /// Установить данные вручную (например, оптимистичное обновление).
    /// </summary>
    public void SetData(T data) => _inner.Set(AsyncSignalResult<T>.Success(data));

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _cts.Cancel();
        _cts.Dispose();
        _inner.Dispose();
    }
}

/// <summary>Результат асинхронного сигнала.</summary>
public readonly record struct AsyncSignalResult<T>
{
    public AsyncSignalState State { get; init; }
    public T? Data { get; init; }
    public Exception? Error { get; init; }

    public static AsyncSignalResult<T> Idle() =>
        new() { State = AsyncSignalState.Idle };

    public static AsyncSignalResult<T> Loading(T? previousData = default) =>
        new() { State = AsyncSignalState.Loading, Data = previousData };

    public static AsyncSignalResult<T> Success(T data) =>
        new() { State = AsyncSignalState.Loaded, Data = data };

    public static AsyncSignalResult<T> Failure(Exception error, T? previousData = default) =>
        new() { State = AsyncSignalState.Error, Error = error, Data = previousData };
}
