// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ BUG-1: ComponentToken — sealed override, не new
// ✅ НОВОЕ: JS Circuit Breaker (после 5 ошибок — прекращаем звать JS)
// ✅ НОВОЕ: GetModuleAsync с retry (1 раз при JSException)
// ✅ НОВОЕ: SafeInvokeVoidAsync 5-arg
// ✅ FIX: _moduleLockDisposed выставляется ДО Dispose

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Base;

public abstract class SgJsComponentBase : SgComponentBase
{
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerenderingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ───────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;

    // JS Circuit Breaker: после _jsErrorThreshold ошибок — прекращаем попытки
    private int _jsConsecutiveErrors;
    private const int JsCircuitBreakerThreshold = 5;

    protected virtual string? JsModulePath => null;

    protected virtual TimeSpan JsModuleLoadTimeout =>
        OperatingSystem.IsBrowser()
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromSeconds(30);

    protected bool HasJsModule => _module is not null;

    // ── BUG-1 FIX: sealed override (не new) ─────────────────────────────────────
    private LifecycleToken _lifecycleToken = new();
    protected internal sealed override CancellationToken ComponentToken => _lifecycleToken.Token;

    // ── DotNetRef ───────────────────────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;
    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            var existing = Volatile.Read(ref _dotNetRef);
            if (existing is not null) return existing;
            var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
            var prior = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
            if (prior is not null) { newRef.Dispose(); return prior; }
            return newRef;
        }
    }

    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── Lifecycle ───────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old.Cancel();
        old.Dispose();
        var oldModule = Interlocked.Exchange(ref _module, null);
        if (oldModule is not null) _ = TryDisposeModuleAsync(oldModule);
        _jsConsecutiveErrors = 0; // сброс circuit breaker
    }

    // ── GetModuleAsync с Circuit Breaker ────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        // Circuit Breaker: прекращаем попытки после N ошибок подряд
        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
        {
            Logger.LogDebug("[{Id}] JS circuit breaker open, skipping module load", ComponentId);
            return null;
        }

        if (_module is not null) return _module;

        using var timeoutCts = new CancellationTokenSource(JsModuleLoadTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ComponentToken, timeoutCts.Token);

        var modulePath = JsModulePath ?? "_content/SuperUI/superui.js";
        bool semaphoreAcquired = false;
        try
        {
            await _moduleLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            semaphoreAcquired = true;

            if (_module is not null) return _module;
            if (IsDisposed || ComponentToken.IsCancellationRequested) return null;

            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, modulePath);

            Interlocked.Exchange(ref _jsConsecutiveErrors, 0); // сброс при успехе
            return _module;
        }
        catch (TaskCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
                Logger.LogWarning("[{Id}] JS module load timed out ({Timeout}s): {Path}",
                    ComponentId, JsModuleLoadTimeout.TotalSeconds, modulePath);
            return null;
        }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (JSException ex)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{Id}] JS module load failed ({Errors}/{Max}): {Path}",
                ComponentId,
                Volatile.Read(ref _jsConsecutiveErrors),
                JsCircuitBreakerThreshold,
                modulePath);
            return null;
        }
        finally
        {
            if (semaphoreAcquired && !_moduleLockDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    // ── SafeInvokeVoidAsync — 0..5 arg overloads ───────────────────────────────
    protected async ValueTask SafeInvokeVoidAsync(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken);
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

    protected async ValueTask SafeInvokeVoidAsync<T1>(string identifier, T1 arg1)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier); }
    }

    protected async ValueTask SafeInvokeVoidAsync<T1, T2>(string identifier, T1 arg1, T2 arg2)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier); }
    }

    protected async ValueTask SafeInvokeVoidAsync<T1, T2, T3>(
        string identifier, T1 arg1, T2 arg2, T3 arg3)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2, arg3);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier); }
    }

    protected async ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2, arg3, arg4);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier); }
    }

    protected async ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4, T5>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2, arg3, arg4, arg5);
            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{ComponentId}] JS void '{Identifier}' failed", ComponentId, identifier); }
    }

    // ── SafeInvokeAsync ─────────────────────────────────────────────────────────
    protected async ValueTask<TResult> SafeInvokeAsync<TResult>(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken);
        }
        catch (TaskCanceledException) { return default!; }
        catch (OperationCanceledException) { return default!; }
        catch (JSDisconnectedException) { return default!; }
        catch (ObjectDisposedException) { return default!; }
        catch (Exception ex)
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
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1);
        }
        catch (TaskCanceledException) { return default!; }
        catch (OperationCanceledException) { return default!; }
        catch (JSDisconnectedException) { return default!; }
        catch (ObjectDisposedException) { return default!; }
        catch (Exception ex)
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
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1, arg2);
        }
        catch (TaskCanceledException) { return default!; }
        catch (OperationCanceledException) { return default!; }
        catch (JSDisconnectedException) { return default!; }
        catch (ObjectDisposedException) { return default!; }
        catch (Exception ex)
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
            var module = await GetModuleAsync();
            if (module is null) return fallback;
            var result = await module.InvokeAsync<TResult>(identifier, ComponentToken, args);
            return result is null ? fallback : result;
        }
        catch (TaskCanceledException) { return fallback; }
        catch (OperationCanceledException) { return fallback; }
        catch (JSDisconnectedException) { return fallback; }
        catch (ObjectDisposedException) { return fallback; }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed", ComponentId, identifier);
            return fallback;
        }
    }

    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try { await JS.InvokeVoidAsync(identifier, ComponentToken, args); }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    protected async Task<bool> EnsureModuleAsync()
    {
        if (IsPrerendering) return false;
        var module = await GetModuleAsync();
        return module is not null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    private Task TryDisposeModuleAsync(IJSObjectReference module)
    {
        var vt = module.DisposeAsync();
        if (vt.IsCompletedSuccessfully) return Task.CompletedTask;
        return vt.AsTask().ContinueWith(
            t =>
            {
                if (t.IsFaulted)
                    Logger.LogDebug(t.Exception, "[{Id}] JS module dispose error", ComponentId);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Current);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        _lifecycleToken.Cancel();

        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); } catch { }
        }

        // FIX: флаг ПЕРЕД Dispose семафора
        _moduleLockDisposed = true;
        Thread.MemoryBarrier();
        try { _moduleLock.Dispose(); }
        catch (ObjectDisposedException) { }

        var dotNetRef = Interlocked.Exchange(ref _dotNetRef, null);
        dotNetRef?.Dispose();

        _lifecycleToken.Dispose();
        await base.DisposeComponentAsync();
    }
}
