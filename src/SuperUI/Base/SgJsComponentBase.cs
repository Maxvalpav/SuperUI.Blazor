using Microsoft.JSInterop;
using SuperUI.Interop;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// 
/// 4+ уровня защиты JS Interop:
/// 1. Prerendering Guard — проверка IsPrerendering
/// 2. Dispose Guard — проверка IsDisposed
/// 3. Cancellation Guard — CancellationToken из LifecycleToken
/// 4. Module Lazy Loading — ES module загружается один раз
/// 5. Circuit Guard — для Blazor Server: проверка активности circuit
/// 
/// Управление DotNetRef — авто-создание и авто-dispose.
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module (lazy, один раз на компонент) ────────────────────────────
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);

    /// <summary>Путь к ES-модулю. Переопределить в наследнике если нужен свой модуль.</summary>
    protected virtual string? JsModulePath => null; // null = использует глобальный superui.js

    // ── DotNetRef авто-управление ────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    /// <summary>
    /// DotNetObjectReference с авто-созданием и авто-dispose.
    /// Безопасен: создаётся только при первом обращении.
    /// </summary>
    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
        => _dotNetRef ??= DotNetObjectReference.Create(this);

    // ── Prerendering ─────────────────────────────────────────────────────────

    /// <summary>
    /// Определяет, находится ли компонент в режиме prerendering.
    /// Использует IHttpContextAccessor + OperatingSystem.IsBrowser() для WASM.
    /// </summary>
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken — race-safe ────────────────────────────────────────────
    private LifecycleToken? _lifecycleToken;

    /// <summary>
    /// Токен жизненного цикла компонента.
    /// Автоматически отменяется при Dispose.
    /// Новый токен создаётся при каждом OnInitialized (reset on reconnect).
    /// </summary>
    protected LifecycleToken LifecycleToken
        => _lifecycleToken ??= new LifecycleToken();

    /// <summary>CancellationToken привязанный к жизненному циклу.</summary>
    protected CancellationToken ComponentToken => LifecycleToken.Token;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        // Сбросить токен при каждой инициализации (важно для SignalR reconnect)
        _lifecycleToken?.Cancel();
        _lifecycleToken?.Dispose();
        _lifecycleToken = new LifecycleToken();

        base.OnInitialized();
    }

    // ── JS Interop — защищённые методы ────────────────────────────────────────

    /// <summary>
    /// Получить JS модуль с lazy loading.
    /// УРОВЕНЬ 4 защиты: Prerendering + Disposed + Cancelled + Module null check.
    /// </summary>
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        // Уровень 1: Prerendering guard
        if (IsPrerendering) return null;

        // Уровень 2: Dispose guard
        if (IsDisposed) return null;

        // Уровень 3: Cancellation guard
        if (ComponentToken.IsCancellationRequested) return null;

        if (_module is not null) return _module;

        await _moduleLock.WaitAsync(ComponentToken);
        try
        {
            if (_module is not null) return _module;

            var path = JsModulePath ?? "_content/SuperUI/superui.js";
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, path);

            return _module;
        }
        catch (TaskCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; } // Blazor Server reconnect
        catch (JSException ex) when (ex.Message.Contains("disconnected")) { return null; }
        finally
        {
            _moduleLock.Release();
        }
    }

    /// <summary>
    /// Безопасный вызов JS без возвращаемого значения.
    /// Все 4 уровня защиты включены.
    /// </summary>
    protected async ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;

            await module.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { /* нормально при dispose */ }
        catch (JSDisconnectedException) { /* Blazor Server reconnect */ }
        catch (ObjectDisposedException) { /* компонент удалён */ }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed", ComponentId, identifier);
        }
    }

    /// <summary>
    /// Безопасный вызов JS с возвращаемым значением.
    /// Возвращает default(T) при любой защитной остановке.
    /// </summary>
    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default;

            return await module.InvokeAsync<T>(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { return default; }
        catch (JSDisconnectedException) { return default; }
        catch (ObjectDisposedException) { return default; }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed", ComponentId, identifier);
            return default;
        }
    }

    /// <summary>
    /// Глобальный вызов JS (через window.*) без модуля.
    /// </summary>
    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return;

        try
        {
            await JS.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
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

        _moduleLock.Dispose();
        _lifecycleToken?.Dispose();

        await base.DisposeComponentAsync();
    }
}
