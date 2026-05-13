// SuperUI/Base/Reactive/SgMiddleware.cs
// НОВЫЙ КЛАСС
// Аналог: Redux middleware, MobX action interceptors
// Поддержка: .NET 8/9/10

namespace SuperUI.Base.Reactive;

/// <summary>
/// Контекст изменения сигнала, передаваемый в middleware.
/// </summary>
public sealed class SgSignalChangeContext<T>
{
    /// <summary>Предыдущее значение.</summary>
    public T PreviousValue { get; init; } = default!;

    /// <summary>Новое значение (можно изменить).</summary>
    public T NewValue { get; set; } = default!;

    /// <summary>Отменить изменение (сигнал останется с PreviousValue).</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>Имя сигнала для логирования.</summary>
    public string? SignalName { get; init; }

    /// <summary>Временная метка изменения.</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>Отменить изменение.</summary>
    public void Cancel() => IsCancelled = true;
}

/// <summary>
/// Делегат middleware обработчика.
/// </summary>
public delegate Task SgSignalMiddlewareDelegate<T>(
    SgSignalChangeContext<T> context,
    Func<SgSignalChangeContext<T>, Task> next);

/// <summary>
/// Сигнал с поддержкой middleware-пайплайна.
/// Позволяет перехватывать, изменять или отменять Set() операции.
///
/// Примеры использования:
/// - Логирование всех изменений
/// - Валидация перед Set()
/// - Optimistic updates с откатом
/// - Rate limiting
///
/// <code>
/// var price = new SgSignalWithMiddleware&lt;decimal&gt;(0m, "price");
/// price.Use(async (ctx, next) =&gt;
/// {
///     if (ctx.NewValue &lt; 0) { ctx.Cancel(); return; }
///     await next(ctx);
/// });
/// price.Use(async (ctx, next) =&gt;
/// {
///     logger.LogInformation("Price: {Old} → {New}", ctx.PreviousValue, ctx.NewValue);
///     await next(ctx);
/// });
/// price.Set(42m);  // пройдёт через оба middleware
/// price.Set(-1m);  // будет отменено первым middleware
/// </code>
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

    /// <summary>Добавить middleware в начало пайплайна (LIFO).</summary>
    public SgSignalWithMiddleware<T> Use(SgSignalMiddlewareDelegate<T> middleware)
    {
        lock (_lock)
            _middlewares.Insert(0, middleware);
        return this;
    }

    /// <summary>Добавить синхронный middleware.</summary>
    public SgSignalWithMiddleware<T> Use(Action<SgSignalChangeContext<T>> middleware)
    {
        return Use(async (ctx, next) =>
        {
            middleware(ctx);
            if (!ctx.IsCancelled)
                await next(ctx);
        });
    }

    /// <summary>Добавить middleware для валидации.</summary>
    public SgSignalWithMiddleware<T> Validate(Func<T, bool> validator, string? errorMessage = null)
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

    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var context = new SgSignalChangeContext<T>
        {
            PreviousValue = _inner.Value,
            NewValue = newValue,
            SignalName = DebugName
        };

        // Если нет middleware — прямой вызов
        SgSignalMiddlewareDelegate<T>[] snapshot;
        lock (_lock) snapshot = [.._middlewares];

        if (snapshot.Length == 0)
        {
            _inner.Set(newValue);
            return;
        }

        // Запускаем пайплайн
        _ = RunPipelineAsync(context, snapshot);
    }

    private async Task RunPipelineAsync(
        SgSignalChangeContext<T> context,
        SgSignalMiddlewareDelegate<T>[] middlewares)
    {
        var index = 0;

        async Task Next(SgSignalChangeContext<T> ctx)
        {
            if (ctx.IsCancelled) return;

            if (index < middlewares.Length)
            {
                var middleware = middlewares[index++];
                await middleware(ctx, Next);
            }
            else
            {
                // Конец пайплайна — применяем изменения
                if (!ctx.IsCancelled)
                    _inner.Set(ctx.NewValue);
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
