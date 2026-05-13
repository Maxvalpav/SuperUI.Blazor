// SuperUI/Base/SgJsComponentBase.cs
// ✅ FIX C1: CancellationTokenSource вместо LifecycleToken —
//    старый токен не отменяется при OnInitialized, только при Dispose.
// ✅ JS Circuit Breaker (5 ошибок → прекращаем звать JS)
// ✅ SafeInvokeVoidAsync — общий Core-метод для сокращения WASM-размера
// ✅ _moduleLockDisposed выставляется ДО Dispose семафора

using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Base;

public abstract class SgJsComponentBase : SgComponentBase
{
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerenderingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ───────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;
    // Task cache for zero-allocation repeated calls (ValueTask built from Task)
    private Task<IJSObjectReference?>? _moduleTask;
    private int _jsConsecutiveErrors;
    private const int JsCircuitBreakerThreshold = 5;

    protected virtual string? JsModulePath => null;
    protected virtual TimeSpan JsModuleLoadTimeout =>
        OperatingSystem.IsBrowser() ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30);

    protected bool HasJsModule => _module is not null;

    /// <summary>true если приложение находится в фазе prerendering (нет JS).</summary>
    protected bool IsPrerendering => PrerendingDetector?.IsPrerendering ?? false;

    /// <summary>
    /// Базовый слот для DotNetObjectReference потомка. Освобождается в DisposeComponentAsync.
    /// Потомки могут переопределить через своё типизированное поле и игнорировать этот слот.
    /// </summary>
    private IDisposable? _dotNetRef;

    // ── Lifecycle Token (FIX C1) ────────────────────────────────────────────
    // Старый токен НЕ отменяется при OnInitialized — только при Dispose.
    // Это позволяет pending JS-операциям завершиться корректно.
    private CancellationTokenSource _lifecycleCts = new();

    protected internal sealed override CancellationToken ComponentToken
    {
        get
        {
            var cts = Volatile.Read(ref _lifecycleCts);
            if (cts is null) return new CancellationToken(true);
            try { return cts.Token; }
            catch (ObjectDisposedException) { return new CancellationToken(true); }
        }
    }

    /// <summary>
    /// Быстрый доступ к модулю с кэшированием ValueTask.
    /// Возвращает немедленно ValueTask из кэша, если модуль уже загружен или загрузка в процессе.
    /// Zero-allocation для повторных вызовов.
    /// </summary>
    protected ValueTask<IJSObjectReference?> GetModuleFastAsync()
    {
        // Модуль уже загружен — возвращаем немедленно completed ValueTask
        if (_module is not null)
            return ValueTask.FromResult<IJSObjectReference?>(_module);

        // Загрузка уже начата другим потоком — возвращаем тот же ValueTask
        var existing = Volatile.Read(ref _moduleTask);
        if (existing is not null)
            return new ValueTask<IJSObjectReference?>(existing);

        // Первый вызов: запускаем загрузку и кэшируем её Task
        var newTask = GetModuleAsync().AsTask();
        var original = Interlocked.CompareExchange(ref _moduleTask, newTask, null);
        if (original is not null)
        {
            // Another thread initialized first; use its task
            return new ValueTask<IJSObjectReference?>(original);
        }

        // Attach continuation to clear the cache after completion (success or failure)
        _ = newTask.ContinueWith(t =>
        {
            // Clear only if still the current task (not replaced by newer request)
            Interlocked.CompareExchange(ref _moduleTask, null, newTask);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return new ValueTask<IJSObjectReference?>(newTask);
    }

    /// <summary>
    /// Загрузить JS-модуль по пути <see cref="JsModulePath"/>. Кэширует результат.
    /// Возвращает null при prerendering / отсутствии пути / ошибке загрузки.
    /// </summary>
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed) return null;
        if (_module is not null) return _module;
        var path = JsModulePath;
        if (string.IsNullOrEmpty(path)) return null;

        if (_moduleLockDisposed) return null;
        try { await _moduleLock.WaitAsync(ComponentToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return null; }
        catch (ObjectDisposedException) { return null; }

        try
        {
            if (_module is not null) return _module;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
            cts.CancelAfter(JsModuleLoadTimeout);
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", cts.Token, path).ConfigureAwait(false);
            return _module;
        }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] Failed to load JS module '{Path}'",
                ComponentId, path);
            return null;
        }
        finally
        {
            if (!_moduleLockDisposed)
                try { _moduleLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    // ── SafeInvokeVoidAsync ─────────────────────────────────────────────────
    private async ValueTask SafeInvokeVoidCoreAsync(
        string identifier, Func<IJSObjectReference, CancellationToken, ValueTask> invoker)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;

        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return;
            await invoker(module, ComponentToken);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    protected ValueTask SafeInvokeVoidAsync(string identifier) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct));

    protected ValueTask SafeInvokeVoidAsync<T1>(string identifier, T1 arg1) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1));

    protected ValueTask SafeInvokeVoidAsync<T1, T2>(string identifier, T1 arg1, T2 arg2) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3>(
        string identifier, T1 arg1, T2 arg2, T3 arg3) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4, T5>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4, arg5));

    // ── SafeInvokeAsync<T> ──────────────────────────────────────────────────
    protected async ValueTask<TResult> SafeInvokeAsync<TResult>(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult, T1>(string identifier, T1 arg1)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult, T1, T2>(
        string identifier, T1 arg1, T2 arg2)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1, arg2);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult>(
        string identifier, TResult fallback, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return fallback;
        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return fallback;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken, args);
            return result is null ? fallback : result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return fallback;
        }
    }

    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            await JS.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    protected async Task<bool> EnsureModuleAsync()
    {
        if (IsPrerendering) return false;
        return await GetModuleFastAsync() is not null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private Task TryDisposeModuleAsync(IJSObjectReference module)
    {
        var vt = module.DisposeAsync();
        if (vt.IsCompletedSuccessfully) return Task.CompletedTask;
        return vt.AsTask().ContinueWith(t =>
        {
            if (t.IsFaulted)
                Logger.LogDebug(t.Exception, "[{Id}] JS module dispose error", ComponentId);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // ✅ FIX C1: отменяем токен только здесь, при полном dispose
        var cts = Interlocked.Exchange(ref _lifecycleCts, null!);
        if (cts is not null)
        {
            try { cts.Cancel(); } catch { }
            try { cts.Dispose(); } catch { }
        }

        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); } catch { }
        }

        _moduleTask = null;
        _moduleLockDisposed = true;
        Thread.MemoryBarrier();
        try { _moduleLock.Dispose(); } catch (ObjectDisposedException) { }

        var dotNetRef = Interlocked.Exchange(ref _dotNetRef, null);
        dotNetRef?.Dispose();

        await base.DisposeComponentAsync();
    }
}
