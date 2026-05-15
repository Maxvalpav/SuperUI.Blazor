// SuperUI/Base/ComponentBases/SgJsComponentBase.cs
// Базовый класс для компонентов с JS-интеропом.
// Устраняет дублирование ~30-50 строк в каждом из ~18 JS-компонентов.
// КЛЮЧЕВОЕ: OnInteractiveAsync вызывается ТОЛЬКО в интерактивном режиме —
// это гарантирует SSR-безопасность.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Базовый класс для компонентов SuperUI, требующих JS-интеропа.
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
/// </list>
/// <para><b>Dispose:</b></para>
/// <list type="number">
///   <item><see cref="IsDisposed"/> = true.</item>
///   <item>Отменяется <see cref="ComponentLifetime"/>.</item>
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
public abstract class SgJsComponentBase : SgComponentBase, IAsyncDisposable
{
    private bool _isDisposed;
    private bool _initialized;
    private CancellationTokenSource? _lifetimeCts;

    // ── Инжекция ──────────────────────────────────────────────────────────────

    /// <summary>Экземпляр JS-рантайма.</summary>
    [Inject]
    protected IJSRuntime JS { get; set; } = default!;

    /// <summary>Scoped-кеш JS-модулей (один import на circuit).</summary>
    [Inject]
    protected SgJsModuleCache ModuleCache { get; set; } = default!;

    // ── Абстрактные члены ─────────────────────────────────────────────────────

    /// <summary>
    /// Путь к JS ES-модулю компонента.
    /// </summary>
    /// <example><c>"./_content/SuperUI/superui-modal.js"</c></example>
    protected abstract string ModulePath { get; }

    // ── Защищённые свойства ───────────────────────────────────────────────────

    /// <summary>
    /// Загруженный JS-модуль. Доступен после <see cref="OnInteractiveAsync"/>.
    /// </summary>
    protected IJSObjectReference? Module { get; private set; }

    /// <summary>
    /// Ссылка на текущий компонент для передачи в JS ([JSInvokable] методы).
    /// Доступна после <see cref="OnInteractiveAsync"/>.
    /// </summary>
    protected DotNetObjectReference<SgJsComponentBase>? SelfRef { get; private set; }

    /// <summary>
    /// <c>true</c>, если компонент уже освобождён.
    /// </summary>
    protected bool IsDisposed => _isDisposed;

    /// <summary>
    /// <c>true</c>, если компонент работает в интерактивном режиме.
    /// </summary>
    protected bool IsInteractive => SgRenderMode.IsInteractive(this);

    /// <summary>
    /// Токен, который отменяется при dispose компонента.
    /// Используйте в долгоживущих операциях внутри компонента.
    /// </summary>
    protected CancellationToken ComponentLifetime
    {
        get
        {
            _lifetimeCts ??= new CancellationTokenSource();
            return _lifetimeCts.Token;
        }
    }

    // ── Lifecycle hooks ───────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается однократно при первом интерактивном рендере.
    /// <para>Здесь выполняется инициализация JS (attach, init, и т.д.).</para>
    /// <para><b>НЕ вызывается</b> при Static SSR и prerender.</para>
    /// </summary>
    protected virtual ValueTask OnInteractiveAsync() => default;

    /// <summary>
    /// Вызывается перед освобождением ресурсов.
    /// <para>Здесь выполняется teardown JS (detach, dispose, и т.д.).</para>
    /// </summary>
    protected virtual ValueTask OnDisposingAsync() => default;

    /// <summary>
    /// SSR-безопасная замена <c>OnAfterRenderAsync</c>.
    /// Вызывается при каждом рендере, но только если компонент не disposed.
    /// </summary>
    protected virtual Task OnAfterRenderSafeAsync(bool firstRender) => Task.CompletedTask;

    // ── OnAfterRenderAsync (sealed) ───────────────────────────────────────────

    /// <inheritdoc/>
    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isDisposed) return;

        await OnAfterRenderSafeAsync(firstRender);

        // Инициализация JS только при первом интерактивном рендере.
        if (firstRender && IsInteractive && !_initialized)
        {
            _initialized = true;
            await InitializeJsAsync();
        }
    }

    // ── Safe invoke ───────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывает JS-функцию модуля, возвращающую значение.
    /// Перехватывает <see cref="JSDisconnectedException"/>,
    /// <see cref="TaskCanceledException"/> и <see cref="ObjectDisposedException"/>.
    /// </summary>
    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
        if (_isDisposed || Module is null) return default;
        try
        {
            return await Module.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>
    /// Вызывает JS-функцию модуля без возвращаемого значения.
    /// Перехватывает типичные исключения disconnected/disposed.
    /// </summary>
    protected async ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (_isDisposed || Module is null) return;
        try
        {
            await Module.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Вызывает глобальную JS-функцию (не из модуля), возвращающую значение.
    /// </summary>
    protected async ValueTask<T?> SafeInvokeAsyncGlobal<T>(string identifier, params object?[] args)
    {
        if (_isDisposed) return default;
        try
        {
            return await JS.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>
    /// Вызывает глобальную JS-функцию без возвращаемого значения.
    /// </summary>
    protected async ValueTask SafeInvokeVoidAsyncGlobal(string identifier, params object?[] args)
    {
        if (_isDisposed) return;
        try
        {
            await JS.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Шаг 1: отменяем ComponentLifetime.
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;

        // Шаг 2: пользовательский teardown (JS detach и т.п.).
        try { await OnDisposingAsync(); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }

        // Шаг 3: dispose DotNetObjectReference.
        var selfRef = SelfRef;
        SelfRef = null;
        selfRef?.Dispose();

        // Шаг 4: НЕ диспозим Module — владеет SgJsModuleCache.
        Module = null;

        GC.SuppressFinalize(this);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task InitializeJsAsync()
    {
        try
        {
            Module = await ModuleCache.GetAsync(JS, ModulePath, ComponentLifetime);
            if (_isDisposed) return;
            SelfRef = DotNetObjectReference.Create(this);
            await OnInteractiveAsync();
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize JS module '{ModulePath}' for {ComponentType}.",
                ModulePath, GetType().Name);
        }
    }
}