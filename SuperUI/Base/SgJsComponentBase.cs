// SuperUI/Base/SgJsComponentBase.cs — ПОЛНЫЙ ИСПРАВЛЕННЫЙ КОД
// FIX C2: CTS обновляется при каждой инициализации
// FIX M4: порядок dispose — сначала base, потом _dotNetRef
// .NET 8/9/10 совместимость

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

    // ════════════════════════════════════════
    // JS Module
    // ════════════════════════════════════════

    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;
    private Task<IJSObjectReference?>? _moduleTask;
    private int _jsConsecutiveErrors;
    private const int JsCircuitBreakerThreshold = 5;

    protected virtual string? JsModulePath => null;
    protected virtual TimeSpan JsModuleLoadTimeout =>
        OperatingSystem.IsBrowser() ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30);

    protected bool HasJsModule => _module is not null;
    protected bool IsPrerendering => PrerendingDetector?.IsPrerendering ?? false;

    /// <summary>
    /// DotNetObjectReference для передачи в JS.
    /// Потомки могут либо использовать этот слот, либо создать свой типизированный.
    /// </summary>
    private IDisposable? _dotNetRef;
    protected IDisposable? DotNetRef
    {
        get => _dotNetRef;
        set
        {
            var old = Interlocked.Exchange(ref _dotNetRef, value);
            old?.Dispose();
        }
    }

    // ════════════════════════════════════════
    // Lifecycle Token (FIX C2)
    // ════════════════════════════════════════

    private CancellationTokenSource? _lifecycleCts;

    protected internal sealed override CancellationToken ComponentToken
    {
        get
        {
            var cts = Volatile.Read(ref _lifecycleCts);
            if (cts is null)
                return new CancellationToken(true);
            try { return cts.Token; }
            catch (ObjectDisposedException) { return new CancellationToken(true); }
        }
    }

    // ════════════════════════════════════════
    // Жизненный цикл
    // ════════════════════════════════════════

    protected override void OnInitialized()
    {
        // FIX C2: создаём НОВЫЙ CTS при каждой инициализации
        // Старый (если был) отменяется
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _lifecycleCts, newCts);
        if (oldCts is not null)
        {
            try { oldCts.Cancel(); } catch { }
            try { oldCts.Dispose(); } catch { }
        }
        base.OnInitialized();
    }

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        // Предзагрузка JS модуля при первом рендере
        if (!IsPrerendering && JsModulePath is not null)
        {
            _ = GetModuleFastAsync();
        }
    }

    // ════════════════════════════════════════
    // GetModuleFastAsync — zero-allocation кэш
    // ════════════════════════════════════════

    protected ValueTask<IJSObjectReference?> GetModuleFastAsync()
    {
        // Модуль уже загружен
        if (_module is not null)
            return ValueTask.FromResult<IJSObjectReference?>(_module);

        // Загрузка уже в процессе
        var existing = Volatile.Read(ref _moduleTask);
        if (existing is not null)
            return new ValueTask<IJSObjectReference?>(existing);

        // Запускаем загрузку
        var newTask = GetModuleAsync().AsTask();
        var original = Interlocked.CompareExchange(ref _moduleTask, newTask, null);
        if (original is not null)
            return new ValueTask<IJSObjectReference?>(original);

        // Очистка кэша после завершения
        _ = newTask.ContinueWith(t =>
        {
            Interlocked.CompareExchange(ref _moduleTask, null, newTask);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return new ValueTask<IJSObjectReference?>(newTask);
    }

    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed)
            return null;

        if (_module is not null)
            return _module;

        var path = JsModulePath;
        if (string.IsNullOrEmpty(path))
            return null;

        if (_moduleLockDisposed)
            return null;

        try
        {
            await _moduleLock.WaitAsync(ComponentToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return null; }
        catch (ObjectDisposedException) { return null; }

        try
        {
            if (_module is not null)
                return _module;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
            cts.CancelAfter(JsModuleLoadTimeout);
            _module = await JS.InvokeAsync<IJSObjectReference>("import", cts.Token, path).ConfigureAwait(false);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
            return _module;
        }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] Failed to load JS module '{Path}'", ComponentId, path);
            return null;
        }
        finally
        {
            if (!_moduleLockDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    // ════════════════════════════════════════
    // SafeInvokeVoidAsync — общий Core
    // ════════════════════════════════════════

    private async ValueTask SafeInvokeVoidCoreAsync(string identifier,
        Func<IJSObjectReference, CancellationToken, ValueTask> invoker)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return;

        // Circuit Breaker
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
        {
            Logger.LogWarning("[{ComponentId}] JS Circuit Breaker open — skipping '{Identifier}'",
                ComponentId, identifier);
            return;
        }

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
            Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier);
        }
    }

    // Сокращённые перегрузки
    protected ValueTask SafeInvokeVoidAsync(string identifier) =>
        SafeInvokeVoidCoreAsync(identifier, (module, ct) => module.InvokeVoidAsync(identifier, ct));

    protected ValueTask SafeInvokeVoidAsync<T1>(string identifier, T1 arg1) =>
        SafeInvokeVoidCoreAsync(identifier, (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1));

    protected ValueTask SafeInvokeVoidAsync<T1, T2>(string identifier, T1 arg1, T2 arg2) =>
        SafeInvokeVoidCoreAsync(identifier, (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3>(string identifier, T1 arg1, T2 arg2, T3 arg3) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4>(string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4, T5>(string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4, arg5));

    // ════════════════════════════════════════
    // SafeInvokeAsync (типизированные)
    // ════════════════════════════════════════

    protected async ValueTask<TResult> SafeInvokeAsync<TResult>(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
            return default!;

        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
            return result;
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
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
            return default!;

        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult, T1, T2>(string identifier, T1 arg1, T2 arg2)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
            return default!;

        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return default!;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1, arg2);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult>(string identifier, TResult fallback, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return fallback;
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
            return fallback;

        try
        {
            var module = await GetModuleFastAsync();
            if (module is null) return fallback;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken, args);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
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
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return;
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

    // ════════════════════════════════════════
    // Dispose (FIX M4: правильный порядок)
    // ════════════════════════════════════════

    protected override async ValueTask DisposeComponentAsync()
    {
        // 1. Отменяем текущий токен
        var cts = Interlocked.Exchange(ref _lifecycleCts, null);
        if (cts is not null)
        {
            try { cts.Cancel(); } catch { }
            try { cts.Dispose(); } catch { }
        }

        // 2. Сначала даём base очиститься (может использовать JS)
        await base.DisposeComponentAsync();

        // 3. Теперь очищаем JS модуль
        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); } catch { }
        }
        _moduleTask = null;

        // 4. Освобождаем семафор
        _moduleLockDisposed = true;
        Thread.MemoryBarrier();
        try { _moduleLock.Dispose(); }
        catch (ObjectDisposedException) { }

        // 5. Освобождаем DotNetRef ПОСЛЕ всего (FIX M4)
        var dotNetRef = Interlocked.Exchange(ref _dotNetRef, null);
        dotNetRef?.Dispose();
    }
}
