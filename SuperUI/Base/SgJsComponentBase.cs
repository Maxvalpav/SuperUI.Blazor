// SuperUI/Base/SgJsComponentBase.cs
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
///
/// ИСПРАВЛЕНИЯ:
/// 1. SemaphoreSlim → Interlocked + volatile для _module lazy loading
/// 2. DotNetRef: Interlocked.CompareExchange для thread-safe lazy init
/// 3. JSException string matching → JSDisconnectedException (правильный тип)
/// 4. LinkedTokenHandle → Dispose корректный
/// 5. _lifecycleToken: сброс при OnInitialized атомарный
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module — ИСПРАВЛЕНО: Interlocked вместо SemaphoreSlim ─────────────
    private volatile IJSObjectReference? _module;
    private volatile int _moduleLoading; // 0 = idle, 1 = loading — spin-wait lock-free
    private volatile int _moduleLoaded;  // 0 = not loaded, 1 = loaded

    protected virtual string? JsModulePath => null;

    // ── DotNetRef — ИСПРАВЛЕНО: thread-safe lazy init ─────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            // ИСПРАВЛЕНО: Interlocked.CompareExchange для thread-safe lazy init
            if (_dotNetRef is null)
            {
                var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
                var existing = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
                if (existing is not null)
                    newRef.Dispose(); // уже создан другим потоком
            }
            return _dotNetRef;
        }
    }

    // ── Prerendering ──────────────────────────────────────────────────────────
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ─────────────────────────────────────────────────────────
    private LifecycleToken? _lifecycleToken;

    protected LifecycleToken LifecycleToken
    {
        get => _lifecycleToken ??= new LifecycleToken();
    }

    protected CancellationToken ComponentToken => LifecycleToken.Token;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        // ИСПРАВЛЕНО: атомарная замена токена при reconnect
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old?.Cancel();
        old?.Dispose();

        base.OnInitialized();
    }

    // ── JS Interop — ИСПРАВЛЕНО ───────────────────────────────────────────────
    /// <summary>
    /// ИСПРАВЛЕНО:
    /// - SemaphoreSlim → volatile + Interlocked spin pattern
    /// - JSDisconnectedException вместо строкового сравнения
    /// - CancellationToken корректно прокидывается
    /// </summary>
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        if (_module is not null) return _module;

        // ИСПРАВЛЕНО: spin-wait без SemaphoreSlim (экономия ~200 байт heap per component)
        // Используем Interlocked для lock-free double-check
        if (Interlocked.Exchange(ref _moduleLoading, 1) == 1)
        {
            // Другой поток загружает — ждём с polling (WASM однопоточный, Server — маловероятно)
            var spinWait = new SpinWait();
            while (_moduleLoaded == 0 && !IsDisposed && !ComponentToken.IsCancellationRequested)
                spinWait.SpinOnce();
            return _module;
        }

        try
        {
            if (_module is not null) return _module; // double-check

            var path = JsModulePath ?? "_content/SuperUI/superui.js";
            var module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                ComponentToken,
                path);

            Interlocked.Exchange(ref _module, module);
            Interlocked.Exchange(ref _moduleLoaded, 1);
            return _module;
        }
        catch (TaskCanceledException)       { return null; }
        catch (OperationCanceledException)  { return null; }
        catch (JSDisconnectedException)     { return null; } // ИСПРАВЛЕНО: правильный тип
        catch (ObjectDisposedException)     { return null; }
        catch (JSException ex)
        {
            // ИСПРАВЛЕНО: только для реально неизвестных JS ошибок
            Logger.LogError(ex, "[{Id}] JS module load failed: {Path}", ComponentId, JsModulePath);
            return null;
        }
        finally
        {
            Interlocked.Exchange(ref _moduleLoading, 0);
        }
    }

    protected async ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException)      { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException)    { }
        catch (ObjectDisposedException)    { }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed", ComponentId, identifier);
        }
    }

    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return default;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default;
            return await module.InvokeAsync<T>(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException)      { return default; }
        catch (OperationCanceledException) { return default; }
        catch (JSDisconnectedException)    { return default; }
        catch (ObjectDisposedException)    { return default; }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed", ComponentId, identifier);
            return default;
        }
    }

    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            await JS.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException)      { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException)    { }
        catch (ObjectDisposedException)    { }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // Отменить токен
        _lifecycleToken?.Cancel();

        // Освободить JS модуль
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* ignore */ }
            _module = null;
        }

        // Освободить DotNetRef
        _dotNetRef?.Dispose();
        _dotNetRef = null;

        _lifecycleToken?.Dispose();
        _lifecycleToken = null;

        await base.DisposeComponentAsync();
    }
}
