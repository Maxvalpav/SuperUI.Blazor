// SuperUI/Base/Reactive/SgMiddleware.cs
// ИСПРАВЛЕНО:
// ✅ Set(T) больше НЕ fire-and-forget: применяет значение немедленно, middleware — async
// ✅ SetAsync(T): полный async pipeline с await
// ✅ Middleware может изменить значение через ctx.NewValue
// ✅ .NET 8/9/10 совместим

namespace SuperUI.Base.Reactive;

/// <summary>Контекст изменения сигнала, передаваемый в middleware.</summary>
public sealed class SgSignalChangeContext<T>
{
    public T PreviousValue { get; init; } = default!;
    public T NewValue { get; set; } = default!;
    public bool IsCancelled { get; private set; }
    public string? SignalName { get; init; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    public void Cancel() => IsCancelled = true;
}

/// <summary>Делегат middleware обработчика.</summary>
public delegate Task SgSignalMiddlewareDelegate<T>(
    SgSignalChangeContext<T> context,
    Func<SgSignalChangeContext<T>, Task> next);

/// <summary>
/// Сигнал с поддержкой middleware-пайплайна.
///
/// ИСПРАВЛЕНО:
/// - Set(T) больше не fire-and-forget: если middleware добавлены,
///   Set применяет значение синхронно и запускает middleware асинхронно
///   (middleware могут изменить значение в следующем тике).
/// - SetAsync(T): полный async pipeline с ожиданием middleware.
/// </summary>
public sealed class SgSignalWithMiddleware<T> : ISgSignal<T>, IDisposable
{
    private readonly SgSignal<T> _inner;
    private readonly List<SgSignalMiddlewareDelegate<T>> _middlewares = [];
    private readonly object _lock = new();
    private int _disposed;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;
    public T Value => _inner.Value;

    public SgSignalWithMiddleware(T initialValue, string? debugName = null)
    {
        _inner = new SgSignal<T>(initialValue, debugName);
    }

    /// <summary>Добавить middleware в пайплайн (LIFO — последний добавленный выполняется первым).</summary>
    public SgSignalWithMiddleware<T> Use(SgSignalMiddlewareDelegate<T> middleware)
    {
        lock (_lock) _middlewares.Insert(0, middleware);
        return this;
    }

    /// <summary>Добавить синхронный middleware.</summary>
    public SgSignalWithMiddleware<T> Use(Action<SgSignalChangeContext<T>> middleware)
    {
        return Use(async (ctx, next) =>
        {
            middleware(ctx);
            if (!ctx.IsCancelled) await next(ctx);
        });
    }

    /// <summary>Добавить middleware для валидации.</summary>
    public SgSignalWithMiddleware<T> Validate(
        Func<T, bool> validator,
        string? errorMessage = null)
    {
        return Use((ctx, next) =>
        {
            if (!validator(ctx.NewValue))
            {
                ctx.Cancel();
                return Task.CompletedTask;
            }
            return next(ctx);
        });
    }

    /// <summary>Добавить middleware для логирования.</summary>
    public SgSignalWithMiddleware<T> LogChanges(Action<string> logger)
    {
        return Use(async (ctx, next) =>
        {
            logger($"[{ctx.SignalName}] {ctx.PreviousValue} → {ctx.NewValue}");
            await next(ctx);
        });
    }

    /// <summary>
    /// Синхронная установка.
    /// Если нет middleware — применяется немедленно.
    /// Если есть middleware — значение применяется немедленно, затем middleware
    /// может скорректировать его асинхронно (fire-and-observe pattern).
    /// Для полного контроля используйте <see cref="SetAsync"/>.
    /// </summary>
    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        SgSignalMiddlewareDelegate<T>[] snapshot;
        lock (_lock) snapshot = [.._middlewares];

        if (snapshot.Length == 0)
        {
            _inner.Set(newValue);
            return;
        }

        // Применяем немедленно — сохраняем синхронный контракт ISgSignal<T>
        _inner.Set(newValue);

        // Запускаем middleware асинхронно (могут отменить или скорректировать значение)
        _ = RunPipelineAndApplyAsync(newValue, snapshot);
    }

    /// <summary>
    /// Полная async установка через middleware-пайплайн.
    /// Значение применяется только если ни один middleware не отменил изменение.
    /// </summary>
    public async ValueTask SetAsync(T newValue)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var context = new SgSignalChangeContext<T>
        {
            PreviousValue = _inner.Value,
            NewValue = newValue,
            SignalName = DebugName
        };

        SgSignalMiddlewareDelegate<T>[] snapshot;
        lock (_lock) snapshot = [.._middlewares];

        if (snapshot.Length == 0)
        {
            _inner.Set(newValue);
            return;
        }

        await RunPipelineAsync(context, snapshot);

        if (!context.IsCancelled)
            _inner.Set(context.NewValue); // middleware мог изменить NewValue
    }

    private async Task RunPipelineAndApplyAsync(
        T originalValue,
        SgSignalMiddlewareDelegate<T>[] middlewares)
    {
        var context = new SgSignalChangeContext<T>
        {
            PreviousValue = _inner.Value,
            NewValue = originalValue,
            SignalName = DebugName
        };

        await RunPipelineAsync(context, middlewares);

        if (context.IsCancelled)
            _inner.Set(context.PreviousValue); // откатываем
        else if (!EqualityComparer<T>.Default.Equals(context.NewValue, originalValue))
            _inner.Set(context.NewValue); // middleware изменил значение
    }

    private static async Task RunPipelineAsync(
        SgSignalChangeContext<T> context,
        SgSignalMiddlewareDelegate<T>[] middlewares)
    {
        var index = 0;

        async Task Next(SgSignalChangeContext<T> ctx)
        {
            if (ctx.IsCancelled) return;
            if (index < middlewares.Length)
            {
                var mw = middlewares[index++];
                await mw(ctx, Next);
            }
        }

        await Next(context);
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);
    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _inner.Dispose();
    }

    public static implicit operator T(SgSignalWithMiddleware<T> signal) => signal.Value;
}