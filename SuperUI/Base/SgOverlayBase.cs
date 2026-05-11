// SuperUI/Base/SgOverlayBase.cs
// ИСПРАВЛЕНО:
// 1. unlockBodyScroll вызывается ПЕРВЫМ в OnCloseCoreAsync
// 2. isDisposePath=true → пропускаем анимацию, используем CancellationToken.None для JS
// 3. _overlayLock: SemaphoreSlim (async-safe, в отличие от volatile bool)
// 4. DisposeComponentAsync: cleanup CTS с таймаутом 5с
// 5. FocusTrapService.ActivateAsync защищена try/catch (prerendering)
// 6. OpenAsync/CloseAsync: защита ObjectDisposedException

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для overlay компонентов (Dialog, Drawer, Tooltip, Popover).
/// Уровень 5: ... → SgInteractiveBase → SgOverlayBase
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
    private volatile bool _isDisposing;

    // ИСПРАВЛЕНО: SemaphoreSlim вместо volatile bool
    // async/await не совместим с volatile bool для sync primitives
    private readonly SemaphoreSlim _overlayLock = new(1, 1);

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }

    protected virtual string AriaRole => "dialog";
    protected virtual int AnimationDurationMs => 300;

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"] = AriaRole,
            ["aria-modal"] = "true",
            ["aria-hidden"] = Open ? "false" : "true"
        };
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
        if (_isDisposing) return;

        // WaitAsync(0) — не блокируем если операция уже выполняется
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

    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.GetNext();

        if (LockBodyScroll)
            await SafeInvokeVoidAsync("lockBodyScroll", null, ComponentId);

        // ИСПРАВЛЕНО: FocusTrap защищён try/catch (может упасть при prerendering)
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
        // Анимация (пропускаем при dispose)
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

        // При isDisposePath ComponentToken уже отменён → используем CancellationToken.None
        var ct = overrideToken ?? (isDisposePath ? CancellationToken.None : ComponentToken);

        // ИСПРАВЛЕНО: 1. СНАЧАЛА unlockBodyScroll — критично для UX
        if (LockBodyScroll)
        {
            try { await SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId); }
            catch { /* JS runtime может быть недоступен */ }
        }

        // 2. Деактивируем focus trap
        if (TrapFocus && _focusTrapId != null)
        {
            try { await FocusTrapService.DeactivateAsync(_focusTrapId); }
            catch { }
            _focusTrapId = null;
        }

        // 3. Освобождаем z-index
        ZIndexService.Release(_zIndex);
        _zIndex = 0;

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync() => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    public async Task OpenAsync()
    {
        if (Open || _isDisposing) return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }

        if (!acquired) return;
        try
        {
            if (Open) return; // double-check
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
        if (!Open || _isDisposing) return;

        bool acquired;
        try { acquired = await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (ObjectDisposedException) { return; }

        if (!acquired) return;
        try
        {
            if (!Open) return; // double-check
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
        _isDisposing = true;

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