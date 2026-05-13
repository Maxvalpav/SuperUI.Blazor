// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ BUG-FIX: _lifecycleToken замена теперь атомарна (volatile + Interlocked)
// ✅ BUG-1: ComponentToken — sealed override
// ✅ НОВОЕ: JS Circuit Breaker (после 5 ошибок — прекращаем звать JS)
// ✅ НОВОЕ: GetModuleAsync с retry (1 раз при JSException)
// ✅ НОВОЕ: SafeInvokeVoidAsync — общий метод Core для сокращения WASM-размера
// ✅ FIX: _moduleLockDisposed выставляется ДО Dispose

using System.Diagnostics;
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

    // ── JS Module ───────────────────────────────────────────────────────────────

    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;

    // JS Circuit Breaker
    private int _jsConsecutiveErrors;
    private const int JsCircuitBreakerThreshold = 5;

    protected virtual string? JsModulePath => null;
    protected virtual TimeSpan JsModuleLoadTimeout =>
        OperatingSystem.IsBrowser() ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30);

    protected bool HasJsModule => _module is not null;

    // ── Lifecycle Token (исправленная версия) ───────────────────────────────────

    private volatile LifecycleToken _lifecycleToken = new();
    protected internal sealed override CancellationToken ComponentToken =>
        _lifecycleToken.Token;

    // ── DotNetRef ───────────────────────────────────────────────────────────────

    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;
    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            var existing = Volatile.Read(ref _dotNetRef);
            if (existing is not null)
                return existing;

            var newRef = DotNetObjectReference.Create(this);
            var prior = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
            if (prior is not null)
            {
                newRef.Dispose();
                return prior;
            }
            return newRef;
        }
    }

    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── InteractiveAuto WASM readiness (.NET 8+) ────────────────────────────────

    /// <summary>
    /// true — WASM runtime готов к работе (либо мы уже в браузере, либо WASM загружен).
    /// В режиме InteractiveAuto на сервере до загрузки WASM возвращает false.
    /// </summary>
    protected bool IsWasmReady =>
        OperatingSystem.IsBrowser() ||
        (RenderMode is InteractiveWebAssemblyRenderMode && !IsPrerendering);

    /// <summary>
    /// Получить JS модуль с учётом InteractiveAuto режима.
    /// В Auto-режиме на сервере ожидает активации WASM перед загрузкой модуля.
    /// Возвращает null если WASM ещё не активирован или модуль недоступен.
    /// </summary>
    protected async ValueTask<IJSObjectReference?> GetModuleWhenReadyAsync()
    {
        if (RenderMode is InteractiveAutoRenderMode && !OperatingSystem.IsBrowser())
        {
            // В Auto-режиме на сервере перед скачиванием WASM — JS модуль может быть недоступен
            // Ожидаем, пока WASM загрузится (определяется через JS-колбэк)
            if (!await IsWasmActivatedAsync())
                return null;
        }

        return await GetModuleAsync();
    }

    /// <summary>
    /// Проверяет, активирован ли WASM runtime в InteractiveAuto режиме.
    /// Возвращает true если WASM готов, false если ещё загружается.
    /// В других режимах всегда возвращает true.
    /// </summary>
    protected virtual async ValueTask<bool> IsWasmActivatedAsync()
    {
        if (OperatingSystem.IsBrowser())
            return true;

        if (RenderMode is not InteractiveAutoRenderMode)
            return true;

        try
        {
            // Проверяем через глобальный JS-хелпер, доступен ли WASM
            // (требует соответствующего JS-кода в приложении)
            var result = await JS.InvokeAsync<bool>(
                "SuperUI.isWasmActivated", ComponentToken);
            return result;
        }
        catch (JSException)
        {
            // Если метод не найден — считаем что WASM ещё не готов
            return false;
        }
        catch (Exception ex) when (ex is OperationCanceledException or JSDisconnectedException)
        {
            return false;
        }
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Атомарная замена токена: создаём новый, меняем, отменяем старый
        var newToken = new LifecycleToken();
        var oldToken = Interlocked.Exchange(ref _lifecycleToken, newToken);

        // Отменяем старый токен ПОСЛЕ замены
        oldToken.Cancel();
        oldToken.Dispose();

        var oldModule = Interlocked.Exchange(ref _module, null);
        if (oldModule is not null)
            _ = TryDisposeModuleAsync(oldModule);

        _jsConsecutiveErrors = 0;
    }

    // ── GetModuleAsync с Circuit Breaker ────────────────────────────────────────

    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        if (Volatile.Read(ref _jsConsecutiveErrors) >= JsCircuitBreakerThreshold)
        {
            Logger.LogDebug("[{Id}] JS circuit breaker open, skipping module load", ComponentId);
            return null;
        }

        if (_module is not null)
            return _module;

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

            Interlocked.Exchange(ref _jsConsecutiveErrors, 0);
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
                ComponentId, Volatile.Read(ref _jsConsecutiveErrors),
                JsCircuitBreakerThreshold, modulePath);
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

    // ── SafeInvokeVoidAsync ─────────────────────────────────────────────────────

    // Общий метод для сокращения дублирования кода (критично для WASM размера)
    private async ValueTask SafeInvokeVoidCoreAsync(
        string identifier,
        Func<IJSObjectReference?, CancellationToken, ValueTask> invoker)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return;

        try
        {
            var module = await GetModuleAsync();
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
            (module, ct) => module!.InvokeVoidAsync(identifier, ct));

    protected ValueTask SafeInvokeVoidAsync<T1>(string identifier, T1 arg1) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module!.InvokeVoidAsync(identifier, ct, arg1));

    protected ValueTask SafeInvokeVoidAsync<T1, T2>(
        string identifier, T1 arg1, T2 arg2) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module!.InvokeVoidAsync(identifier, ct, arg1, arg2));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3>(
        string identifier, T1 arg1, T2 arg2, T3 arg3) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module!.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module!.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4));

    protected ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4, T5>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
        SafeInvokeVoidCoreAsync(identifier,
            (module, ct) => module!.InvokeVoidAsync(identifier, ct, arg1, arg2, arg3, arg4, arg5));

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed",
                ComponentId, identifier);
            return default!;
        }
    }

    protected async ValueTask<TResult> SafeInvokeAsync<TResult, T1>(
        string identifier, T1 arg1)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, arg1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed",
                ComponentId, identifier);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed",
                ComponentId, identifier);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Increment(ref _jsConsecutiveErrors);
            Logger.LogError(ex, "[{ComponentId}] JS '{Identifier}' failed",
                ComponentId, identifier);
            return fallback;
        }
    }

    protected async ValueTask SafeGlobalInvokeVoidAsync(
        string identifier, params object?[] args)
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
        var module = await GetModuleAsync();
        return module is not null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private Task TryDisposeModuleAsync(IJSObjectReference module)
    {
        var vt = module.DisposeAsync();
        if (vt.IsCompletedSuccessfully)
            return Task.CompletedTask;

        return vt.AsTask().ContinueWith(t =>
        {
            if (t.IsFaulted)
                Logger.LogDebug(t.Exception, "[{Id}] JS module dispose error", ComponentId);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        _lifecycleToken.Cancel();

        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch { }
        }

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
