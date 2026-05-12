// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНО:
// 1. DotNetRef: защита от исключения в DotNetObjectReference.Create
// 2. SafeInvokeVoidAsync: zero-arg overload без params-аллокации
// 3. SafeInvokeAsync: zero-arg overload
// 4. DisposeComponentAsync: try/finally для гарантированного Dispose семафора
// 5. _moduleLockDisposed: volatile для видимости между потоками
// 6. GetModuleAsync: проверка IsDisposed ПОСЛЕ получения семафора (double-check)
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// </summary>
/// <remarks>
/// Уровень 2: ComponentBase → SgComponentBase → SgJsComponentBase
///
/// JS модуль загружается лениво через <see cref="GetModuleAsync"/> с таймаутом 30 сек.
/// Все JS-вызовы безопасны при prerendering, dispose и потере соединения (Server).
/// </remarks>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ─────────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ─────────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;

    /// <summary>Путь к JS ESM модулю. Если null — используется superui.js.</summary>
    protected virtual string? JsModulePath => null;

    // ── DotNetRef ─────────────────────────────────────────────────────────────────
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
                newRef.Dispose(); // проиграли гонку — диспозим свой
                return prior;
            }
            return newRef;
        }
    }

    // ── Prerendering ──────────────────────────────────────────────────────────────
    /// <summary>true во время статического prerendering (SSR) — JS недоступен.</summary>
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ────────────────────────────────────────────────────────────
    private LifecycleToken _lifecycleToken = new();
    protected CancellationToken ComponentToken => _lifecycleToken.Token;

    protected override void OnInitialized()
    {
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old.Cancel();
        old.Dispose();

        // При hot-reload сбрасываем модуль (будет переинициализирован при следующем вызове)
        var oldModule = Interlocked.Exchange(ref _module, null);
        if (oldModule is not null)
            _ = TryDisposeModuleAsync(oldModule);

        base.OnInitialized();
    }

    // ── GetModuleAsync ────────────────────────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        // Быстрый путь: модуль уже загружен
        if (_module is not null) return _module;

        // ИСПРАВЛЕНО: таймаут 30с + linked token
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ComponentToken, timeoutCts.Token);

        bool semaphoreAcquired = false;
        try
        {
            await _moduleLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            semaphoreAcquired = true;

            // Double-check после получения семафора
            if (_module is not null) return _module;
            if (IsDisposed || ComponentToken.IsCancellationRequested) return null;

            var path = JsModulePath ?? "_content/SuperUI/superui.js";
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, path);

            return _module;
        }
        catch (TaskCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
                Logger.LogWarning("[{Id}] JS module load timed out (30s): {Path}",
                    ComponentId, JsModulePath);
            return null;
        }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (JSException ex)
        {
            Logger.LogError(ex, "[{Id}] JS module load failed: {Path}",
                ComponentId, JsModulePath);
            return null;
        }
        finally
        {
            // ИСПРАВЛЕНО: ВСЕГДА освобождаем семафор если был получен
            if (semaphoreAcquired && !_moduleLockDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { /* семафор уже задиспожен */ }
            }
        }
    }

    // ── SafeInvokeVoidAsync ───────────────────────────────────────────────────────
    /// <summary>Вызов JS void-функции без аргументов (без аллокации массива).</summary>
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

    /// <summary>Вызов JS void-функции с произвольными аргументами.</summary>
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
        finally
        {
            Diagnostics.TotalJsMs += Stopwatch.GetElapsedTime(sw).TotalMilliseconds;
        }
#endif
    }

    /// <summary>Zero-allocation overload для одного типизированного аргумента.</summary>
    protected ValueTask SafeInvokeVoidAsync<TArg>(string identifier, TArg arg)
        => SafeInvokeVoidAsync(identifier, null, arg);

    // ── SafeInvokeAsync ───────────────────────────────────────────────────────────
    /// <summary>Вызов JS функции с возвращаемым значением. Zero-arg overload.</summary>
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

    /// <summary>Вызов JS функции с возвращаемым значением и аргументами.</summary>
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

    // ── SafeGlobalInvokeVoidAsync ─────────────────────────────────────────────────
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

    // ── Helpers ───────────────────────────────────────────────────────────────────
    private static async Task TryDisposeModuleAsync(IJSObjectReference module)
    {
        try { await module.DisposeAsync(); }
        catch { /* JS runtime может быть недоступен */ }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // 1. Отменяем токен — все текущие JS вызовы получат OperationCanceledException
        _lifecycleToken.Cancel();

        // 2. Диспозим JS модуль
        // ИСПРАВЛЕНО: try/finally гарантирует освобождение семафора даже при исключении
        var module = Interlocked.Exchange(ref _module, null);
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch { /* JS runtime может быть уже недоступен */ }
        }

        // 3. Диспозим семафор. Флаг выставляем ПОСЛЕ Dispose,
        // чтобы GetModuleAsync не увидел disposed=false при уже задиспоженом семафоре.
        try { _moduleLock.Dispose(); }
        catch (ObjectDisposedException) { }
        finally { _moduleLockDisposed = true; }

        // 4. Диспозим DotNetRef
        var dotNetRef = Interlocked.Exchange(ref _dotNetRef, null);
        dotNetRef?.Dispose();

        // 5. Диспозим LifecycleToken
        _lifecycleToken.Dispose();

        await base.DisposeComponentAsync();
    }
}