// SuperUI/Base/SgOverlayBase.cs
// Ключевые исправления относительно версии до 20.5:
// 1. OpenAsync/CloseAsync — добавлен IsDisposed check
// 2. ZIndexService.Release — в try/finally (_zIndex = 0 всегда)
// 3. BuildAriaAttributes — HasAriaModal виртуальное свойство
// 4. HasAriaModal = false для Tooltip/Popover (переопределять в дочерних)

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для overlay компонентов (Dialog, Drawer, Tooltip, Popover).
/// Уровень 5: SgInteractiveBase → SgOverlayBase
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
    private int _isDisposingInt; // 0 = false, 1 = true
    private int _animationDurationMs = 300; // fallback значение

    private readonly SemaphoreSlim _overlayLock = new(1, 1);

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }

    protected virtual string AriaRole => "dialog";
    /// <summary>
    /// Длительность анимации открытия/закрытия в миллисекундах.
    /// Значение синхронизируется с CSS-переменной на первом рендере через JS.
    /// Override для кастомной логики.
    /// </summary>
    protected virtual int AnimationDurationMs => _animationDurationMs;

    /// <summary>
    /// Добавлять aria-modal="true" в BuildAriaAttributes.
    /// Tooltip/Popover переопределяют на false.
    /// </summary>
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
        OnKey("Escape", async () =>
        {
            if (CloseOnEscape && Open) await CloseAsync();
        });
        base.OnInitialized();
    }

     protected override async Task OnParametersSetAsync()
     {
         await base.OnParametersSetAsync();
         if (Interlocked.Exchange(ref _isDisposingInt, 0) == 1) return;

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
         // Синхронизируем длительность анимации с CSS, если возможно
         if (!IsPrerendering)
         {
             var ms = await SafeInvokeAsync<int, string>("getAnimationDuration", ComponentId);
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
            try
            {
                await Task.Delay(AnimationDurationMs, overrideToken ?? ComponentToken);
            }
            catch (OperationCanceledException) { }
            IsAnimatingClose = false;
        }

        var ct = overrideToken ?? (isDisposePath ? CancellationToken.None : ComponentToken);

        if (LockBodyScroll)
        {
            try { await SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId); }
            catch { }
        }

        if (TrapFocus && _focusTrapId != null)
        {
            try { await FocusTrapService.DeactivateAsync(_focusTrapId); }
            catch { }
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
        if (Open || Interlocked.Exchange(ref _isDisposingInt, 0) == 1 || IsDisposed) return;

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
            try { _overlayLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    public async Task CloseAsync()
    {
        if (!Open || Interlocked.Exchange(ref _isDisposingInt, 0) == 1) return;

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
            try { _overlayLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick) await CloseAsync();
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        if (Interlocked.Exchange(ref _isDisposingInt, 1) == 1) return; // идемпотентность

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
