// SuperUI/Base/ComponentBases/SgOverlayComponentBase.cs
// Базовый класс для оверлеев: Modal, Drawer, Popover, Tooltip, ToastHost.
// Убирает паттерн _previousVisible/Allocate/Release из каждого оверлея.

using Microsoft.AspNetCore.Components;
using SuperUI.Services;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Базовый класс для компонентов-оверлеев SuperUI.
/// </summary>
/// <remarks>
/// <para>Управляет:</para>
/// <list type="bullet">
///   <item>Z-index (через <see cref="SgZIndexService"/>): Allocate при открытии, Release при закрытии.</item>
///   <item>Состоянием visible/closing: <see cref="IsOpening"/>, <see cref="IsClosing"/>.</item>
///   <item>Lifecycle-хуками: OnOpeningAsync, OnOpenedAsync, OnClosingAsync, OnClosedAsync.</item>
///   <item>Стандартным каналом закрытия: <see cref="RequestCloseAsync"/> (вызывается из JS через ESC/backdrop).</item>
/// </list>
/// <para><b>Использование (пример миграции SgModal):</b></para>
/// <code>
/// public partial class SgModal : SgOverlayComponentBase
/// {
///     protected override string ModulePath => "./_content/SuperUI/superui-modal.js";
///     protected override int ZIndexBase => SgZIndexService.ModalBase;
///     protected override string IdPrefix => "sg-modal";
///
///     protected override async ValueTask OnOpeningAsync()
///     {
///         await SafeInvokeVoidAsync("attach", RootRef, SelfRef, CloseOnEscape);
///         if (Draggable) await SafeInvokeVoidAsync("initDrag", RootRef, _headerRef);
///     }
///
///     protected override async ValueTask OnClosingAsync()
///     {
///         await SafeInvokeVoidAsync("detach");
///     }
///
///     // [JSInvokable] метод — стандартный канал ESC/backdrop:
///     [JSInvokable] public override Task RequestCloseAsync() => CloseAsync();
/// }
/// </code>
/// </remarks>
public abstract class SgOverlayComponentBase : SgJsComponentBase
{
    private bool _previousVisible;
    private bool _isOpening;
    private bool _isClosing;
    private int _zIndexValue;
    private CancellationTokenSource? _animationCts;

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Управляет видимостью оверлея.
    /// </summary>
    [Parameter]
    public bool Visible { get; set; }

    /// <summary>
    /// Callback при изменении <see cref="Visible"/>.
    /// </summary>
    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    // ── Инжекция ──────────────────────────────────────────────────────────────

    /// <summary>Сервис управления z-index.</summary>
    [Inject]
    protected SgZIndexService ZIndex { get; set; } = default!;

    // ── Абстрактные члены ─────────────────────────────────────────────────────

    /// <summary>
    /// Базовый z-index для данного типа оверлея.
    /// Используйте константы <see cref="SgZIndexService"/>:
    /// <see cref="SgZIndexService.ModalBase"/>, <see cref="SgZIndexService.TooltipBase"/>, и т.д.
    /// </summary>
    protected abstract int ZIndexBase { get; }

    // ── Защищённые свойства ───────────────────────────────────────────────────

    /// <summary>Текущий выделенный z-index. 0 — оверлей закрыт.</summary>
    protected int ZIndexValue => _zIndexValue;

    /// <summary>Z-index бэкдропа (на 5 ниже оверлея).</summary>
    protected int BackdropZIndex => _zIndexValue > 0 ? _zIndexValue - 5 : 0;

    /// <summary><c>true</c> во время анимации открытия.</summary>
    protected bool IsOpening => _isOpening;

    /// <summary><c>true</c> во время анимации закрытия.</summary>
    protected bool IsClosing => _isClosing;

    // ── Lifecycle hooks ───────────────────────────────────────────────────────

    /// <summary>Вызывается перед показом оверлея (до анимации).</summary>
    protected virtual ValueTask OnOpeningAsync() => default;

    /// <summary>Вызывается после завершения анимации открытия.</summary>
    protected virtual ValueTask OnOpenedAsync() => default;

    /// <summary>Вызывается перед скрытием оверлея (до анимации закрытия).</summary>
    protected virtual ValueTask OnClosingAsync() => default;

    /// <summary>Вызывается после завершения анимации закрытия.</summary>
    protected virtual ValueTask OnClosedAsync() => default;

    /// <summary>
    /// Длительность анимации закрытия в миллисекундах.
    /// После истечения — <see cref="VisibleChanged"/> вызывается с <c>false</c>.
    /// По умолчанию 200 мс.
    /// </summary>
    protected virtual int ClosingAnimationMs => 200;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Стандартный канал закрытия из JS (ESC, backdrop click).
    /// Помечен <c>[JSInvokable]</c> — Blazor резолвит по runtime-типу.
    /// </summary>
    [Microsoft.JSInterop.JSInvokable]
    public virtual Task RequestCloseAsync() => CloseAsync();

    /// <summary>
    /// Программно закрывает оверлей.
    /// </summary>
    public async Task CloseAsync()
    {
        if (IsDisposed || _isClosing || !Visible) return;

        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        _isClosing = true;
        StateHasChanged();

        await OnClosingAsync();

        try
        {
            await Task.Delay(ClosingAnimationMs, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        _isClosing = false;
        _previousVisible = false;
        ReleaseZIndex();

        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);

        await OnClosedAsync();
    }

    // ── Override OnInteractiveAsync ──────────────────────────────────────────

    /// <inheritdoc/>
    protected override async ValueTask OnInteractiveAsync()
    {
        // Если компонент уже создан с Visible=true (типичный кейс prerender→interactive
        // или просто открытый по умолчанию модал), выполняем полный цикл открытия здесь
        // и помечаем _previousVisible, чтобы OnAfterRenderSafeAsync не запустил его повторно.
        if (Visible && !_previousVisible)
        {
            _previousVisible = true;
            _zIndexValue = ZIndex.Allocate(this, ZIndexBase);
            StateHasChanged();
            await OnOpeningAsync();
            await OnOpenedAsync();
        }
    }

    // ── Override OnAfterRenderSafeAsync ───────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!IsInteractive) return; // SSR-guard

        // Detect Visible → true (opening)
        if (Visible && !_previousVisible)
        {
            _previousVisible = true;
            _zIndexValue = ZIndex.Allocate(this, ZIndexBase);
            _isOpening = true;
            StateHasChanged();
            await OnOpeningAsync();
            _isOpening = false;
            await OnOpenedAsync();
        }
        // Detect Visible → false (closing via parameter, not CloseAsync)
        else if (!Visible && _previousVisible)
        {
            _previousVisible = false;
            ReleaseZIndex();
            await OnClosingAsync();
            await OnClosedAsync();
        }
    }

    // ── Override OnDisposingAsync ─────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ValueTask OnDisposingAsync()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        ReleaseZIndex();
        return default;
    }

    /// <summary>
    /// Перевыделяет z-index для оверлея — освобождает текущий и берёт новый,
    /// который окажется поверх остальных оверлеев того же базового уровня.
    /// Используйте для реализации «bring to front» (например, в плавающих окнах).
    /// </summary>
    protected void BringToFront()
    {
        if (!Visible || IsDisposed) return;
        if (_zIndexValue > 0) ZIndex.Release(this);
        _zIndexValue = ZIndex.Allocate(this, ZIndexBase);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void ReleaseZIndex()
    {
        if (_zIndexValue > 0)
        {
            ZIndex.Release(this);
            _zIndexValue = 0;
        }
    }
}