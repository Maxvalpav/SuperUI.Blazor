using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.Services;

namespace SuperUI.Base;

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
    private volatile bool _isProcessingOverlay;

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }
    protected virtual string AriaRole => "dialog";
    protected virtual int AnimationDurationMs => 300;

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"]       = AriaRole,
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
        if (_isProcessingOverlay) return;

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

    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.GetNext();
        if (LockBodyScroll)
            await SafeInvokeVoidAsync("lockBodyScroll", ComponentId);
        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            await FocusTrapService.ActivateAsync(_focusTrapId);
        }
        await OnOpenAsync();
        StateHasChanged();
    }

    private async Task OnCloseCoreAsync(bool animate = true, CancellationToken? overrideToken = null)
    {
        if (animate)
        {
            IsAnimatingClose = true;
            StateHasChanged();

            using var animCts = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(AnimationDurationMs + 100));
            try
            {
                await Task.Delay(AnimationDurationMs, animCts.Token);
            }
            catch (OperationCanceledException) { }
            IsAnimatingClose = false;
        }

        var ct = overrideToken ?? ComponentToken;

        if (TrapFocus && _focusTrapId != null)
        {
            try
            {
                if (!ct.IsCancellationRequested)
                    await FocusTrapService.DeactivateAsync(_focusTrapId);
            }
            catch { /* ignore during dispose */ }
            _focusTrapId = null;
        }

        if (LockBodyScroll)
        {
            try
            {
                if (!ct.IsCancellationRequested)
                    await SafeInvokeVoidAsync("unlockBodyScroll", ComponentId);
            }
            catch { /* ignore during dispose */ }
        }

        ZIndexService.Release(_zIndex);
        _zIndex = 0;
        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync() => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    public async Task OpenAsync()
    {
        if (Open || _isProcessingOverlay) return;
        _isProcessingOverlay = true;
        try
        {
            Open = true;
            _wasOpen = true;
            await OpenChanged.InvokeAsync(true);
            await OnOpenCoreAsync();
        }
        finally
        {
            _isProcessingOverlay = false;
        }
    }

    public async Task CloseAsync()
    {
        if (!Open || _isProcessingOverlay) return;
        _isProcessingOverlay = true;
        try
        {
            Open = false;
            _wasOpen = false;
            await OpenChanged.InvokeAsync(false);
            await OnCloseCoreAsync(animate: true);
        }
        finally
        {
            _isProcessingOverlay = false;
        }
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick) await CloseAsync();
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        if (Open || _focusTrapId != null || _zIndex > 0)
        {
            using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await OnCloseCoreAsync(animate: false, overrideToken: cleanupCts.Token);
        }
        await base.DisposeComponentAsync();
    }
}
