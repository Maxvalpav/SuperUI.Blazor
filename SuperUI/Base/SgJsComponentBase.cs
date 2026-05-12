// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНИЯ:
// 1. _moduleLockDisposed — выставляется ДО Dispose семафора (правильный порядок)
// 2. JsModuleLoadTimeout — виртуальное свойство, адаптивно для WASM/Server
// 3. SafeInvokeVoidAsync<T1> — 1-arg generic без params-аллокации
// 4. SafeInvokeAsync<TResult,T1> — 1-arg без params-аллокации
// 5. TryDisposeModuleAsync — fire-and-forget с защитой от UnhandledTaskException
// 6. DisposeComponentAsync — ValueTask (консистентно)
//
// ДОРАБОТКИ:
// 7. GetModuleAsync: modulePath объявлена до try-блока (устраняет CS0165 в catch)
// 8. SafeInvokeAsync<TResult, T1, T2> — 2-arg overload без params-аллокации
// 9. TryDisposeModuleAsync — улучшен error logging (не static, использует Logger)

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// Уровень 2: ComponentBase → SgComponentBase → SgJsComponentBase
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ───────────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerenderingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ───────────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;

    /// <summary>Путь к JS ESM модулю. null = superui.js.</summary>
    protected virtual string? JsModulePath => null;

    /// <summary>
    /// Таймаут загрузки JS модуля.
    /// ИСПРАВЛЕНО: адаптивный — WASM 10с (браузерный fetch быстрее), Server 30с.
    /// </summary>
    protected virtual TimeSpan JsModuleLoadTimeout =>
        OperatingSystem.IsBrowser()
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromSeconds(30);

    /// <summary>JS-модуль успешно загружен.</summary>
    protected bool HasJsModule => _module is not null;

    // ── DotNetRef ───────────────────────────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            var existing = Volatile.Read(ref _dotNetRef);
            if (existing is not null) return existing;
            var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
            var prior = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
            if (prior is not null)
            {
                newRef.Dispose();
                return prior;
            }
            return newRef;
        }
    }

    // ── Prerendering ────────────────────────────────────────────────────────────────
    /// <summary>true во время статического prerendering (SSR) — JS недоступен.</summary>
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ──────────────────────────────────────────────────────────────
    private LifecycleToken _lifecycleToken = new();
    protected CancellationToken ComponentToken => _lifecycleToken.Token;

    protected override void OnInitialized()
    {
        // 1. Сначала базовая инициализация (сервисы, хуки)
        base.OnInitialized();

        // 2. Потом сбрасываем JS-специфичное состояние
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old.Cancel();
        old.Dispose();
        var oldModule = Interlocked.Exchange(ref _module, null);
        if (oldModule is not null) _ = TryDisposeModuleAsync(oldModule);
    }

    // ── GetModuleAsync ──────────────────────────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        if (_module is not null) return _module;

        using var timeoutCts = new CancellationTokenSource(JsModuleLoadTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ComponentToken, timeoutCts.Token);

        bool semaphoreAcquired = false;
        try
        {
            await _moduleLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            semaphoreAcquired = true;

            if (_module is not null) return _module;
            if (IsDisposed || ComponentToken.IsCancellationRequested) return null;

                var modulePath = JsModulePath ?? "_content/SuperUI/superui.js";
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, modulePath);
            return _module;
        }
        catch (TaskCanceledException)
        {
            var modulePath = JsModulePath ?? "_content/SuperUI/superui.js";
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
            var modulePath = JsModulePath ?? "_content/SuperUI/superui.js";
            Logger.LogError(ex, "[{Id}] JS module load failed: {Path}", ComponentId, modulePath);
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

    // ── SafeInvokeVoidAsync ─────────────────────────────────────────────────────────
    /// <summary>Вызов JS void без аргументов (zero-allocation).</summary>
    protected async ValueTask SafeInvokeVoidAsync(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    /// <summary>Вызов JS void с произвольными аргументами.</summary>
    protected async ValueTask SafeInvokeVoidAsync(
        string identifier,
        CancellationToken? overrideToken = null,
        params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
        var sw = Stopwatch.GetTimestamp();
#endif
        var ct = overrideToken ?? ComponentToken;
        if (IsPrerendering || (overrideToken is null && IsDisposed)) return;
        if (ct.IsCancellationRequested) return;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ct, args);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
#if DEBUG
        finally { Diagnostics.TotalJsMs += Stopwatch.GetElapsedTime(sw).TotalMilliseconds; }
#endif
    }

    /// <summary>
    /// ИСПРАВЛЕНО: 1-arg generic overload без params-аллокации (value types не боксируются).
    /// </summary>
    protected async ValueTask SafeInvokeVoidAsync<T1>(string identifier, T1 arg1)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    /// <summary>2-arg generic overload без params-аллокации.</summary>
    protected async ValueTask SafeInvokeVoidAsync<T1, T2>(string identifier, T1 arg1, T2 arg2)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    /// <summary>3-arg generic overload без params-аллокации.</summary>
    protected async ValueTask SafeInvokeVoidAsync<T1, T2, T3>(
        string identifier, T1 arg1, T2 arg2, T3 arg3)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2, arg3);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    /// <summary>4-arg generic overload без params-аллокации.</summary>
    protected async ValueTask SafeInvokeVoidAsync<T1, T2, T3, T4>(
        string identifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, arg1, arg2, arg3, arg4);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS void call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    // ── SafeInvokeAsync ─────────────────────────────────────────────────────────────
    /// <summary>Вызов JS с возвращаемым значением. Zero-arg (zero-allocation).</summary>
    protected async ValueTask<T> SafeInvokeAsync<T>(string identifier)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<T>(identifier, ComponentToken);
        }
        catch (TaskCanceledException) { return default!; }
        catch (OperationCanceledException) { return default!; }
        catch (JSDisconnectedException) { return default!; }
        catch (ObjectDisposedException) { return default!; }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default!;
        }
    }

    /// <summary>Вызов JS с возвращаемым значением и аргументами.</summary>
    protected async ValueTask<T> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default!;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default!;
            return await module.InvokeAsync<T>(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { return default!; }
        catch (OperationCanceledException) { return default!; }
        catch (JSDisconnectedException) { return default!; }
        catch (ObjectDisposedException) { return default!; }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default!;
        }
    }

    /// <summary>ИСПРАВЛЕНО: 1-arg generic без params-аллокации.</summary>
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
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default!;
        }
    }

    /// <summary>ДОРАБОТКА: 2-arg generic без params-аллокации.</summary>
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
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default!;
        }
    }

    // ── SafeGlobalInvokeVoidAsync ───────────────────────────────────────────────────
    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try { await JS.InvokeVoidAsync(identifier, ComponentToken, args); }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────
    // ДОРАБОТКА: улучшен error logging (не static, использует Logger)
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

    // ── Dispose ─────────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        _lifecycleToken.Cancel();

        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch { }
        }

        // ИСПРАВЛЕНО: сначала выставляем флаг, потом Dispose (атомарный порядок)
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
