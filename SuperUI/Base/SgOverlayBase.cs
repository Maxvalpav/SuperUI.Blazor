using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для overlay компонентов (Dialog, Drawer, Tooltip, Popover).
/// Уровень 5: SgInteractiveBase → SgOverlayBase
///
/// ИСПРАВЛЕНО:
/// 1. OnInitialized — base.OnInitialized() ПЕРВЫМ (хук Escape регистрируется после).
/// 2. _animationDurationMs — volatile (пишется из OnFirstRenderAsync, читается из рендера).
/// 3. OpenAsync/CloseAsync — IsDisposed check.
/// 4. ZIndexService.Release — в try/finally (_zIndex=0 всегда).
/// 5. HasAriaModal — виртуальное свойство (false для Tooltip/Popover).
/// 6. aria-placeholder удалён (нестандартный атрибут).
/// </summary>
public abstract class SgOverlayBase : SgInteractiveBase
{
    [Inject] protected IZIndexService ZIndexService { get; set; } = null!;
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = null!;

    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;
    [Parameter] public bool LockBodyScroll { get; set; } = true;
    [Parameter] public bool TrapFocus { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private int _zIndex;
    private bool _wasOpen;
    private string? _focusTrapId;
    private int _isDisposingInt;
    // ИСПРАВЛЕНО: volatile — пишется из OnFirstRenderAsync, читается из ShouldRender/рендера
    private volatile int _animationDurationMs = 300;
    private readonly SemaphoreSlim _overlayLock = new(1, 1);

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }
    protected virtual string AriaRole => "dialog";
    protected virtual int AnimationDurationMs => _animationDurationMs;

    /// Добавлять aria-modal="true". Tooltip/Popover переопределяют на false.
    protected virtual bool HasAriaModal => true;

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"] = AriaRole,
            ["aria-hidden"] = Open ? "false" : "true"
        };
        if (HasAriaModal) attrs["aria-modal"] = "true";
        return attrs;
    }

    protected override void OnInitialized()
    {
        // ИСПРАВЛЕНО: base.OnInitialized() ПЕРВЫМ — инициализирует _keyHandlers
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
        finally { _overlayLock.Release(); }
    }

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (!IsPrerendering)
        {
            var ms = await SafeInvokeAsync<int>("getAnimationDuration", ComponentId);
            if (ms > 0) _animationDurationMs = ms;
        }
    }

    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.GetNext();

        if (LockBodyScroll)
            await SafeInvokeVoidAsync("lockBodyScroll", null, ComponentId);

        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            try { await FocusTrapService.ActivateAsync(_focusTrapId); }
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
            try { await Task.Delay(AnimationDurationMs, overrideToken ?? ComponentToken); }
            catch (OperationCanceledException) { }
            IsAnimatingClose = false;
        }

        var ct = overrideToken ?? (isDisposePath ? CancellationToken.None : ComponentToken);

        if (LockBodyScroll)
        {
            try { await SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId); } catch { }
        }

        if (TrapFocus && _focusTrapId != null)
        {
            try { await FocusTrapService.DeactivateAsync(_focusTrapId); } catch { }
            _focusTrapId = null;
        }

        // ИСПРАВЛЕНО: Release в try/finally — _zIndex=0 всегда
        try { ZIndexService.Release(_zIndex); }
        finally { _zIndex = 0; }

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync() => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    public async Task OpenAsync()
    {
        // ИСПРАВЛЕНО: IsDisposed check
        if (Open || IsDisposed || Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1)
            return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }
        if (!acquired) return;

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
            try { _overlayLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    public async Task CloseAsync()
    {
        if (!Open || IsDisposed || Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1)
            return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }
        if (!acquired) return;

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
            try { _overlayLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick) await CloseAsync();
    }

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
