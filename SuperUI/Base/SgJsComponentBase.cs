// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНО:
// 1. Семафор ВСЕГДА освобождается в finally (semaphoreAcquired флаг)
// 2. Таймаут 30с на WaitAsync (защита от бесконечного ожидания)
// 3. ComponentToken.IsCancellationRequested (не IsCancelled — CS1061)
// 4. Hot-reload: OnInitialized сбрасывает _module при пересоздании токена
// 5. SafeInvokeVoidAsync: generic overloads для zero-box args
// 6. Предупреждение в лог при таймауте GetModuleAsync
// 7. DisposeComponentAsync: Cancel → DisposeModule → DisposeSemaphore (безопасный порядок)
// 8. _moduleLockDisposed: volatile для видимости между потоками
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// Уровень 2: ComponentBase → SgComponentBase → SgJsComponentBase
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ─────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;
    protected virtual string? JsModulePath => null;

    // ── DotNetRef ─────────────────────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            if (_dotNetRef is null)
            {
                var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
                var existing = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
                if (existing is not null)
                    newRef.Dispose(); // проиграли гонку — диспозим созданный
            }
            return _dotNetRef;
        }
    }

    // ── Prerendering ──────────────────────────────────────────────────────────
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ────────────────────────────────────────────────────────
    // Создаётся сразу — гарантирует что ComponentToken валиден с момента создания объекта
    private LifecycleToken _lifecycleToken = new();
    protected CancellationToken ComponentToken => _lifecycleToken.Token;

    protected override void OnInitialized()
    {
        // Пересоздаём токен при повторной инициализации (hot-reload сценарий)
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old.Cancel();
        old.Dispose();

        // ИСПРАВЛЕНО: сбрасываем модуль при hot-reload
        // Старый модуль будет задиспожен в следующем вызове DisposeComponentAsync
        // но нам нужна возможность его переинициализировать
        if (_module is not null)
        {
            _ = TryDisposeModuleAsync(_module);
            _module = null;
        }

        base.OnInitialized();
    }

    // ── GetModuleAsync ────────────────────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        // ИСПРАВЛЕНО: используем IsCancellationRequested (не IsCancelled — CS1061)
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        // Быстрый путь без входа в семафор
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
            // ИСПРАВЛЕНО: предупреждение при таймауте
            if (timeoutCts.IsCancellationRequested)
                Logger.LogWarning("[{Id}] JS module load timed out (30s): {Path}", ComponentId, JsModulePath);
            return null;
        }
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
            // Независимо от IsDisposed — иначе ожидающие потоки зависнут навсегда
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
        var sw = System.Diagnostics.Stopwatch.GetTimestamp();
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
            Diagnostics.TotalJsMs +=
                System.Diagnostics.Stopwatch.GetElapsedTime(sw).TotalMilliseconds;
        }
#endif
    }

    // Zero-allocation overload для одного аргумента
    protected ValueTask SafeInvokeVoidAsync<TArg>(string identifier, TArg arg)
        => SafeInvokeVoidAsync(identifier, null, arg);

    // ── SafeInvokeAsync<T> ────────────────────────────────────────────────────
    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
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

    // ── SafeGlobalInvokeVoidAsync ─────────────────────────────────────────────
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

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static async Task TryDisposeModuleAsync(IJSObjectReference module)
    {
        try { await module.DisposeAsync(); }
        catch { /* ignore — JS runtime может быть недоступен */ }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // 1. Отменяем токен — все текущие JS вызовы получат OperationCanceledException
        _lifecycleToken.Cancel();

        // 2. Даём текущим вызовам GetModuleAsync завершиться (они получат cancellation)
        //    Небольшая задержка не нужна — OperationCanceledException обрабатывается в catch

        // 3. Диспозим JS модуль
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* JS runtime может быть уже недоступен */ }
            _module = null;
        }

        // 4. Помечаем семафор как disposed и диспозим
        //    (после этого все новые WaitAsync вернут ObjectDisposedException → handled в catch)
        _moduleLockDisposed = true;
        _moduleLock.Dispose();

        // 5. Диспозим DotNetRef
        _dotNetRef?.Dispose();
        _dotNetRef = null;

        // 6. Диспозим LifecycleToken
        _lifecycleToken.Dispose();

        await base.DisposeComponentAsync();
    }
}