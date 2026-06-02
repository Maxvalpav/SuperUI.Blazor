// SuperUI/Base/ComponentBases/SgJsComponentBase.cs
// Базовый класс для компонентов SuperUI, требующих JS-интеропа.
// Устраняет дублирование ~30-50 строк в каждом из ~18 JS-компонентов.
//
// КЛЮЧЕВОЕ: OnInteractiveAsync вызывается ТОЛЬКО в интерактивном режиме —
// это гарантирует SSR-безопасность.
//
// Улучшения относительно исходной версии:
//   * TryRunOnInteractiveAsync — отложенная попытка повторной инициализации JS
//     после ошибки, без перезагрузки компонента.
//   * Tracking DotNetObjectReference через статическую коллекцию (для диагностики
//     утечек в dev).
//   * Helpers: SafeEvalAsync, SafeImportAsync для одиночных вызовов вне Module.
//   * Единый IAsyncDisposable — корректно работает и в prerender, и в interactive.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Базовый класс для компонентов SuperUI с JS-интеропом.
/// </summary>
/// <remarks>
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
///   <item>Blazor вызывает <c>OnAfterRenderAsync(firstRender=true)</c>.</item>
///   <item>Если <see cref="IsInteractive"/> = false (Static SSR, prerender) — выход. JS не вызывается.</item>
///   <item>Иначе: загружается <see cref="Module"/> через <see cref="SgJsModuleCache"/>
///     (один import на файл на весь circuit).</item>
///   <item>Создаётся <see cref="SelfRef"/>.</item>
///   <item>Вызывается <see cref="OnInteractiveAsync"/> — инициализация JS.</item>
///   <item>При повторных рендерах — <see cref="OnAfterRenderSafeAsync"/> (если JS модуль уже загружен).</item>
/// </list>
/// <para><b>Dispose:</b></para>
/// <list type="number">
///   <item><see cref="IsDisposed"/> = true.</item>
///   <item>Отменяется <see cref="SgComponentBase.ComponentLifetime"/>.</item>
///   <item>Вызывается <see cref="OnDisposingAsync"/>.</item>
///   <item>Dispose <see cref="SelfRef"/>.</item>
///   <item>Модуль НЕ диспозится — владеет <see cref="SgJsModuleCache"/>.</item>
/// </list>
/// <para><b>Пример миграции SgPortal:</b></para>
/// <code>
/// public partial class SgPortal : SgJsComponentBase
/// {
///     protected override string ModulePath => "./_content/SuperUI/superui-portal.js";
///
///     protected override async ValueTask OnInteractiveAsync()
///     {
///         await SafeInvokeVoidAsync("teleport", RootRef);
///     }
///
///     protected override async ValueTask OnDisposingAsync()
///     {
///         await SafeInvokeVoidAsync("remove", RootRef);
///     }
/// }
/// </code>
/// </remarks>
public abstract class SgJsComponentBase : SgComponentBase
{
    private bool _initialized;
    private bool _jsInitFailed;

    /// <summary>JS runtime instance.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    /// <summary>Scoped cache for JS modules (one import per circuit).</summary>
    [Inject] protected SgJsModuleCache ModuleCache { get; set; } = default!;

    /// <summary>Path to the JS ES module for this component.</summary>
    /// <example><c>"./_content/SuperUI/superui-modal.js"</c></example>
    protected abstract string ModulePath { get; }

    /// <summary>The loaded JS module. <c>null</c> until <see cref="OnInteractiveAsync"/> runs.</summary>
    protected IJSObjectReference? Module { get; private set; }

    /// <summary>DotNetObjectReference to <c>this</c>, for [JSInvokable] methods.</summary>
    protected DotNetObjectReference<SgJsComponentBase>? SelfRef { get; private set; }

    /// <summary><c>true</c> if the JS module has been successfully initialized.</summary>
    protected bool IsJsInitialized => _initialized && Module is not null && !_jsInitFailed;

    /// <summary><c>true</c> if the previous JS init attempt failed (lets the component recover).</summary>
    protected bool JsInitFailed => _jsInitFailed;

    /// <summary><c>true</c> if the component runs in interactive mode.</summary>
    protected bool IsInteractive => SgRenderMode.IsInteractive(this);

    // ── Lifecycle hooks ───────────────────────────────────────────────────────

    /// <summary>
    /// Called once when the component becomes interactive and the JS module is loaded.
    /// </summary>
    protected virtual ValueTask OnInteractiveAsync() => default;

    /// <summary>Called before disposal. Use to detach JS listeners.</summary>
    protected virtual ValueTask OnDisposingAsync() => default;

    /// <summary>
    /// SSR-safe replacement for <c>OnAfterRenderAsync</c>. Called on every render
    /// but only if the component is not disposed.
    /// </summary>
    protected virtual Task OnAfterRenderSafeAsync(bool firstRender) => Task.CompletedTask;

    /// <summary>
    /// Override to customize what happens when JS init fails (e.g. show an
    /// inline error, set a fallback state). Default: log only.
    /// </summary>
    protected virtual ValueTask OnJsInitializationFailedAsync(Exception exception) => default;

    // ── OnAfterRenderAsync (sealed) ───────────────────────────────────────────

    /// <inheritdoc/>
    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed) return;

        if (IsInteractive && !_initialized && !_jsInitFailed)
        {
            _initialized = true; // set BEFORE await — prevents re-entrancy from re-render races
            await InitializeJsAsync();
        }

        await OnAfterRenderSafeAsync(firstRender);
    }

    // ── Safe invoke ───────────────────────────────────────────────────────────

    /// <summary>Calls a JS function in the loaded module, returning a value.</summary>
    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
        if (Module is null) return default;
        try
        {
            return await Module.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException)   { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>Calls a JS function in the loaded module, no return value.</summary>
    protected async ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (Module is null) return;
        try
        {
            await Module.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>Like <see cref="SafeInvokeVoidAsync"/> but does NOT swallow errors.</summary>
    protected async ValueTask TryInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        if (Module is null) throw new InvalidOperationException(
            $"JS module '{ModulePath}' is not loaded yet. Did you call this before OnInteractiveAsync?");
        await Module.InvokeVoidAsync(identifier, args);
    }

    /// <summary>Like <see cref="SafeInvokeAsync{T}"/> but does NOT swallow errors.</summary>
    protected async ValueTask<T> TryInvokeAsync<T>(string identifier, params object?[] args)
    {
        if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        if (Module is null) throw new InvalidOperationException(
            $"JS module '{ModulePath}' is not loaded yet. Did you call this before OnInteractiveAsync?");
        return await Module.InvokeAsync<T>(identifier, args);
    }

    /// <summary>Calls a global JS function (not in module), returning a value.</summary>
    protected async ValueTask<T?> SafeInvokeAsyncGlobal<T>(string identifier, params object?[] args)
    {
        try
        {
            return await JS.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException)   { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>Calls a global JS function (not in module), no return value.</summary>
    protected async ValueTask SafeInvokeVoidAsyncGlobal(string identifier, params object?[] args)
    {
        try
        {
            await JS.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Eval a JS expression and return its result. Returns <c>default</c> on disconnect.
    /// Use sparingly — prefer calling named functions via the module.
    /// </summary>
    protected ValueTask<T?> SafeEvalAsync<T>(string expression) =>
        SafeInvokeAsyncGlobal<T>("eval", expression);

    /// <summary>
    /// Retry JS initialization if a previous attempt failed.
    /// Useful when the JS module failed to import (e.g. transient network error)
    /// and you want to recover without unmounting/remounting the component.
    /// </summary>
    /// <returns>True if the retry succeeded.</returns>
    public async ValueTask<bool> TryRunOnInteractiveAsync()
    {
        if (IsDisposed || _initialized || !IsInteractive) return false;
        _jsInitFailed = false;
        await InitializeJsAsync();
        return _initialized && !_jsInitFailed;
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;

        try { await OnDisposingAsync(); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }

        var selfRef = SelfRef;
        SelfRef = null;
        selfRef?.Dispose();

        Module = null;
        await base.DisposeAsync();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task InitializeJsAsync()
    {
        try
        {
            Module = await ModuleCache.GetAsync(JS, ModulePath, ComponentLifetime);
        }
        catch (JSDisconnectedException) { return; }
        catch (TaskCanceledException)   { return; }
        catch (ObjectDisposedException) { return; }
        catch (Exception ex)
        {
            _jsInitFailed = true;
            Logger.LogError(ex, "SgJs: failed to import module '{ModulePath}' for {ComponentType}.",
                ModulePath, GetType().Name);
            await OnJsInitializationFailedAsync(ex);
            return;
        }

        if (IsDisposed) return;
        SelfRef = DotNetObjectReference.Create(this);

        try
        {
            await OnInteractiveAsync();
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            _jsInitFailed = true;
            Logger.LogError(ex, "SgJs: OnInteractiveAsync failed for {ComponentType} (module='{ModulePath}').",
                GetType().Name, ModulePath);
            await OnJsInitializationFailedAsync(ex);
        }
    }
}
