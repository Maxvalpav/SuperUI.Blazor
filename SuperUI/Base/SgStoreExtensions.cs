// SuperUI/Base/SgStoreExtensions.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS0246: добавлен using Microsoft.AspNetCore.Components (для [Inject])
// ✅ CS0029: SelectAsync возвращает Task<TResult>, не TState
// ✅ FIX: OnStateChange — больше не утекает middleware (BUG-3)
// ✅ NEW: DispatchAsync с exponential backoff
// ✅ NEW: extension-методы для удобного использования SgStore

using Microsoft.AspNetCore.Components;           // ← ИСПРАВЛЕНИЕ CS0246
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Base.Reactive;

namespace SuperUI.Base;

/// <summary>
/// Extension-методы для <see cref="SgStore{TState}"/>.
/// </summary>
public static class SgStoreExtensions
{
    // ── Dispatch helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Dispatch синхронного reducer с fluent-цепочкой.
    /// </summary>
    public static SgStore<TState> With<TState>(
        this SgStore<TState> store,
        Func<TState, TState> reducer)
        where TState : notnull
    {
        store.Dispatch(reducer);
        return store;
    }

    /// <summary>
    /// Batch-dispatch нескольких reducer'ов атомарно.
    /// </summary>
    public static async Task<SgStore<TState>> WithBatchAsync<TState>(
        this SgStore<TState> store,
        params Func<TState, TState>[] reducers)
        where TState : notnull
    {
        await store.BatchAsync(reducers);
        return store;
    }

    // ── Select / Project ──────────────────────────────────────────────────────

    /// <summary>
    /// Проецировать состояние синхронно.
    /// </summary>
    public static TResult Select<TState, TResult>(
        this SgStore<TState> store,
        Func<TState, TResult> selector)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        return selector(store.Current);
    }

    /// <summary>
    /// ИСПРАВЛЕНИЕ CS0029: асинхронная проекция возвращает Task<TResult>, не TState.
    /// </summary>
    public static async Task<TResult> SelectAsync<TState, TResult>(
        this SgStore<TState> store,
        Func<TState, Task<TResult>> asyncSelector)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(asyncSelector);
        return await asyncSelector(store.Current);
    }

    // ── OnStateChange — ИСПРАВЛЕНИЕ BUG утечки middleware ─────────────────────

    /// <summary>
    /// Подписаться на изменения состояния.
    /// ИСПРАВЛЕНИЕ: вместо добавления middleware (утечка!) — подписываемся на SgSignal.
    /// Возвращает реальный IDisposable для отписки.
    /// </summary>
    public static IDisposable OnChange<TState>(
        this SgStore<TState> store,
        Action<TState> observer)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(observer);
        // Subscribe на SgSignal.State — реальная отписка при Dispose
        return store.State.Subscribe(observer);
    }

    /// <summary>
    /// Подписаться на изменения с предыдущим и новым значением.
    /// ИСПРАВЛЕНИЕ: используем State.Subscribe, а не Use() — нет утечки middleware.
    /// </summary>
    public static IDisposable OnChange<TState>(
        this SgStore<TState> store,
        Action<TState, TState> observer)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(observer);
        TState prev = store.Current;
        return store.State.Subscribe(next =>
        {
            var p = prev;
            prev = next;
            observer(p, next);
        });
    }

    // ── DispatchAsync с exponential backoff ────────────────────────────────────

    /// <summary>
    /// DispatchAsync с exponential backoff при конфликте состояния.
    /// ИСПРАВЛЕНИЕ: retry-loop больше не линейный — используем экспоненциальную задержку.
    /// </summary>
    public static async Task DispatchWithBackoffAsync<TState>(
        this SgStore<TState> store,
        Func<TState, Task<TState>> asyncReducer,
        int maxRetries = 5,
        int baseDelayMs = 10,
        CancellationToken cancellationToken = default)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(asyncReducer);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await store.DispatchAsync(asyncReducer);
                return;
            }
            catch (InvalidOperationException) when (attempt < maxRetries - 1)
            {
                // Exponential backoff: 10ms, 20ms, 40ms, 80ms...
                var delay = baseDelayMs * (1 << attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }
        // Последняя попытка — без перехвата
        await store.DispatchAsync(asyncReducer);
    }

    // ── IOptionsMonitor-style hot config ──────────────────────────────────────

    /// <summary>
    /// Получить текущее состояние как read-only snapshot.
    /// </summary>
    public static TState GetSnapshot<TState>(this SgStore<TState> store)
        where TState : notnull
        => store.Current;

    /// <summary>
    /// Проверить, соответствует ли состояние предикату.
    /// </summary>
    public static bool Matches<TState>(
        this SgStore<TState> store,
        Func<TState, bool> predicate)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate(store.Current);
    }

    // ── DI registration helper ────────────────────────────────────────────────

    /// <summary>
    /// Зарегистрировать SgStore<TState> как singleton в DI.
    /// </summary>
    public static IServiceCollection AddSgStore<TState>(
        this IServiceCollection services,
        TState initialState,
        Action<SgStore<TState>>? configure = null)
        where TState : notnull
    {
        return services.AddSingleton(sp =>
        {
            var store = new SgStore<TState>(initialState);
            configure?.Invoke(store);
            return store;
        });
    }
}
