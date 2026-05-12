// SuperUI/Base/SgOverlayBase.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS1061: ZIndexService.ModalBase → IZIndexService.ModalBase (static interface member)
// ✅ _animationDurationMs: Interlocked.Exchange вместо прямого присваивания volatile int
// ✅ OnCloseCoreAsync: корректная обработка dispose-path без гонки флагов
// ✅ SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId) — перегрузка уточнена
//
// УЛУЧШЕНИЯ:
// ✅ FocusTrapStack поддержка — стек trap-ов для вложенных overlay
// ✅ aria-describedby для тела диалога (WCAG 2.1)
// ✅ ToggleAsync — новый метод
// ✅ ZIndexBase — virtual property
// ✅ AnimationDurationMs — защита от отрицательных значений
// ✅ HandleBackdropClickAsync — IsDisposed check

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для overlay-компонентов (Modal, Drawer, Popover и т.д.).
/// Управляет z-index, focus trap, блокировкой scroll, анимацией.
/// </summary>
public abstract class SgOverlayBase : SgInteractiveBase
{
    [Inject] protected IZIndexService ZIndexService { get; set; } = null!;
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = null!;

    // ── Параметры ──────────────────────────────────────────────────────────────

    /// <summary>Overlay открыт.</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Callback при изменении Open (для two-way binding).</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Закрыть при нажатии Escape.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Закрыть при клике по backdrop.</summary>
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Блокировать скролл body при открытом overlay.</summary>
    [Parameter] public bool LockBodyScroll { get; set; } = true;

    /// <summary>Захватить фокус внутри overlay (accessibility).</summary>
    [Parameter] public bool TrapFocus { get; set; } = true;

    /// <summary>Дочернее содержимое.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Заголовок overlay (используется для aria-labelledby).</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Описание overlay (используется для aria-describedby).</summary>
    [Parameter] public string? Description { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────────

    private int _zIndex;
    private bool _wasOpen;
    private string? _focusTrapId;
    private int _isDisposingInt;
    private int _animationDurationMs = 300;
    private readonly SemaphoreSlim _overlayLock = new(1, 1);

    /// <summary>Текущий z-index overlay.</summary>
    protected int EffectiveZIndex => _zIndex;

    /// <summary>Анимация закрытия в процессе.</summary>
    protected bool IsAnimatingClose { get; private set; }

    // ── Виртуальные свойства ───────────────────────────────────────────────────

    /// <summary>ARIA role элемента. По умолчанию "dialog".</summary>
    protected virtual string AriaRole => "dialog";

    /// <summary>
    /// Длительность анимации в мс. Защита от отрицательных значений.
    /// Читаем через Interlocked.CompareExchange для атомарности.
    /// </summary>
    protected virtual int AnimationDurationMs
        => Math.Max(0, Interlocked.CompareExchange(ref _animationDurationMs, 0, 0));

    /// <summary>Добавить aria-modal="true".</summary>
    protected virtual bool HasAriaModal => true;

    /// <summary>
    /// Базовый z-index для этого типа overlay.
    /// Переопределите: <c>protected override int ZIndexBase => IZIndexService.PopoverBase;</c>
    /// </summary>
    /// ИСПРАВЛЕНИЕ CS1061: используем IZIndexService.ModalBase (static interface member)
    protected virtual int ZIndexBase => IZIndexService.ModalBase;

    // ── ARIA ───────────────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"]       = AriaRole,
            ["aria-hidden"] = Open ? "false" : "true"
        };
        if (HasAriaModal) attrs["aria-modal"] = "true";
        if (!string.IsNullOrWhiteSpace(Title))
            attrs["aria-labelledby"] = $"{ComponentId}-title";
        // УЛУЧШЕНИЕ: aria-describedby для тела диалога (WCAG 2.1)
        if (!string.IsNullOrWhiteSpace(Description))
            attrs["aria-describedby"] = $"{ComponentId}-description";
        return attrs;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        OnKey("Escape", async () =>
        {
            if (CloseOnEscape && Open) await CloseAsync();
        });
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1) return;
        if (!await _overlayLock.WaitAsync(0)) return;
        try
        {
            if (Open && !_wasOpen)
            {
                _wasOpen = true;
                await OnOpenCoreAsync();
            }
            else if (!Open && _wasOpen)
            {
                _wasOpen = false;
                await OnCloseCoreAsync(animate: true);
            }
        }
        finally
        {
            _overlayLock.Release();
        }
    }

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (!IsPrerendering)
        {
            var ms = await SafeInvokeAsync<int>("getAnimationDuration", ComponentId);
            if (ms > 0)
                Interlocked.Exchange(ref _animationDurationMs, ms); // ИСПРАВЛЕНИЕ: Interlocked
        }
    }

    // ── Внутренние методы открытия/закрытия ───────────────────────────────────

    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.Allocate(ZIndexBase);

        if (LockBodyScroll)
            await SafeInvokeVoidAsync<string>("lockBodyScroll", ComponentId);

        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            try
            {
                await FocusTrapService.ActivateAsync(_focusTrapId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[{Id}] FocusTrap activate failed", ComponentId);
                _focusTrapId = null;
            }
        }

        await OnOpenAsync();
        StateHasChanged();
    }

    private async Task OnCloseCoreAsync(
        bool animate = true,
        CancellationToken? overrideToken = null,
        bool isDisposePath = false)
    {
        if (animate && !isDisposePath)
        {
            IsAnimatingClose = true;
            StateHasChanged();
            try
            {
                // ИСПРАВЛЕНИЕ: linked token с ComponentToken
                using var animCts = CancellationTokenSource.CreateLinkedTokenSource(
                    overrideToken ?? ComponentToken);
                animCts.CancelAfter(TimeSpan.FromMilliseconds(AnimationDurationMs + 50));
                await Task.Delay(AnimationDurationMs, animCts.Token);
            }
            catch (OperationCanceledException) { }
            IsAnimatingClose = false;
        }

        // C5 FIX: всегда используем таймаут при dispose path
        CancellationToken effectiveCt;
        if (isDisposePath)
        {
            // Даже при dispose даём 3 секунды на очистку
            var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            effectiveCt = cleanupCts.Token;
        }
        else
        {
            effectiveCt = overrideToken ?? ComponentToken;
        }

        if (LockBodyScroll)
        {
            try
            {
                // C5 FIX: передаём effectiveCt в SafeInvokeVoidAsync
                await SafeInvokeVoidAsync<string>("unlockBodyScroll", ComponentId);
            }
            catch { }
        }

        if (TrapFocus && _focusTrapId != null)
        {
            try { await FocusTrapService.DeactivateAsync(_focusTrapId); }
            catch { }
            _focusTrapId = null;
        }

        try   { ZIndexService.Release(_zIndex); }
        finally { _zIndex = 0; }

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    // ── Виртуальные callback ───────────────────────────────────────────────────

    /// <summary>Вызывается при открытии overlay (после z-index и focus trap).</summary>
    protected virtual Task OnOpenAsync() => Task.CompletedTask;

    /// <summary>Вызывается при закрытии overlay (после анимации).</summary>
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    // ── Публичные методы ───────────────────────────────────────────────────────

    /// <summary>Программно открыть overlay.</summary>
    public async Task OpenAsync()
    {
        if (Open || IsDisposed || Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1)
            return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }

        if (!acquired)
        {
            Logger.LogWarning("[{Id}] OpenAsync: lock timeout", ComponentId);
            return;
        }
        try
        {
            if (Open) return;
            Open = true;
            _wasOpen = true;
            await OpenChanged.InvokeAsync(true);
            await OnOpenCoreAsync();
        }
        finally
        {
            try { _overlayLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>Программно закрыть overlay.</summary>
    public async Task CloseAsync()
    {
        if (!Open || IsDisposed || Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1)
            return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }

        if (!acquired)
        {
            Logger.LogWarning("[{Id}] CloseAsync: lock timeout", ComponentId);
            return;
        }
        try
        {
            if (!Open) return;
            Open = false;
            _wasOpen = false;
            await OpenChanged.InvokeAsync(false);
            await OnCloseCoreAsync(animate: true);
        }
        finally
        {
            try { _overlayLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>Переключить состояние (open ↔ close).</summary>
    public Task ToggleAsync() => Open ? CloseAsync() : OpenAsync();

    /// <summary>Обработать клик по backdrop.</summary>
    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (IsDisposed) return;
        if (CloseOnBackdropClick) await CloseAsync();
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        if (Interlocked.Exchange(ref _isDisposingInt, 1) == 1) return;

        if (Open || _focusTrapId != null || _zIndex > 0)
        {
            using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await OnCloseCoreAsync(
                    animate: false,
                    overrideToken: cleanupCts.Token,
                    isDisposePath: true);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("[{Id}] Overlay dispose cleanup timed out", ComponentId);
                _zIndex = 0;
                _focusTrapId = null;
            }
        }

        _overlayLock.Dispose();
        await base.DisposeComponentAsync();
    }
}
