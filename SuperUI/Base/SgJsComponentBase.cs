// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНО:
// 1. Семафор ВСЕГДА освобождается в finally (даже при IsDisposed=true)
// 2. Добавлен таймаут 30с на WaitAsync чтобы не зависать вечно
// 3. LifecycleToken инициализируется в конструкторе (не лениво) — thread-safe
// 4. DisposeComponentAsync: порядок операций безопасен (сначала Cancel, потом Dispose модуля)
// 5. DotNetRef: Interlocked.CompareExchange уже корректен (оставлен)
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// 
/// ИСПРАВЛЕНИЯ:
/// - Семафор ВСЕГДА освобождается в finally блоке
/// - Таймаут на ожидание семафора (30 секунд)
/// - LifecycleToken создаётся в OnInitialized (не лениво) для thread-safety
/// - Правильный порядок dispose: Cancel → DisposeModule → DisposeSemaphore
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ───────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ──────────────────────────────────────────────────────────────
    // Семафор: не более одной параллельной загрузки модуля
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;
    protected virtual string? JsModulePath => null;

    // ── DotNetRef ──────────────────────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            if (_dotNetRef is null)
            {
                var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
                var existing = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
                if (existing is not null) newRef.Dispose(); // проиграли гонку — диспозим созданный
            }
            return _dotNetRef;
        }
    }

    // ── Prerendering ───────────────────────────────────────────────────────────
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ─────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: создаётся в конструкторе, не лениво
    // Это гарантирует что ComponentToken всегда валиден с момента создания объекта
    private LifecycleToken _lifecycleToken = new();
    protected CancellationToken ComponentToken => _lifecycleToken.Token;

    protected override void OnInitialized()
    {
        // Пересоздаём токен при повторной инициализации (hot-reload сценарий)
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old.Cancel();
        old.Dispose();
        base.OnInitialized();
    }

    // ── GetModuleAsync ─────────────────────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancelled)
            return null;

        // Быстрый путь без входа в семафор
        if (_module is not null) return _module;

        // ИСПРАВЛЕНО: используем CancellationTokenSource с таймаутом 30с
        // чтобы не зависнуть если семафор никогда не освободится
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

            if (IsDisposed || ComponentToken.IsCancelled)
                return null;

            var path = JsModulePath ?? "_content/SuperUI/superui.js";
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, path);

            return _module;
        }
        catch (TaskCanceledException) { return null; }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (JSException ex)
        {
            Logger.LogError(ex, "[{Id}] JS module load failed: {Path}", ComponentId, JsModulePath);
            return null;
        }
        finally
        {
            // ИСПРАВЛЕНО: ВСЕГДА освобождаем семафор если он был получен
            // Независимо от IsDisposed — иначе ожидающие потоки зависнут
            if (semaphoreAcquired && !_moduleLockDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { /* семафор уже задиспожен — нормально */ }
            }
        }
    }

    // ── SafeInvokeVoidAsync ───────────────────────────────────────────────────
    protected async ValueTask SafeInvokeVoidAsync(
        string identifier,
        CancellationToken? overrideToken = null,
        params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        // ИСПРАВЛЕНО: при Dispose используем overrideToken, игнорируем ComponentToken
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
    }

    // ── SafeInvokeAsync<T> ─────────────────────────────────────────────────────
    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancelled)
            return default;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default;
            return await module.InvokeAsync<T>(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { return default; }
        catch (OperationCanceledException) { return default; }
        catch (JSDisconnectedException) { return default; }
        catch (ObjectDisposedException) { return default; }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default;
        }
    }

    // ── SafeGlobalInvokeVoidAsync ──────────────────────────────────────────────
    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancelled) return;

        try
        {
            await JS.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    // ── Dispose ────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // 1. Отменяем токен — все текущие JS вызовы получат OperationCanceledException
        _lifecycleToken.Cancel();

        // 2. Диспозим JS модуль (безопасно — все вызовы уже прерваны токеном)
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* JS runtime может быть уже недоступен */ }
            _module = null;
        }

        // 3. Помечаем семафор как disposed и диспозим его
        // (после этого все WaitAsync получат ObjectDisposedException → handled)
        _moduleLockDisposed = true;
        _moduleLock.Dispose();

        // 4. Диспозим DotNetRef
        _dotNetRef?.Dispose();
        _dotNetRef = null;

        // 5. Диспозим LifecycleToken
        _lifecycleToken.Dispose();

        await base.DisposeComponentAsync();
    }
}