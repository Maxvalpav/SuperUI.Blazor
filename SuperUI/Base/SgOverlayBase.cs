// SuperUI/Base/SgOverlayBase.cs
// ИСПРАВЛЕНО:
// 1. Dispose — SafeInvokeVoidAsync вызывается с overrideToken (не ComponentToken!)
// 2. _isProcessingOverlay — Interloaded вместо volatile bool для Server-safety
// 3. OnCloseCoreAsync принимает CancellationToken напрямую

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

    // ИСПРАВЛЕНО: int для Interlocked.CompareExchange (0=false, 1=true)
    private volatile int _isProcessingOverlay;

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }
    protected virtual string AriaRole => "dialog";
    protected virtual int AnimationDurationMs => 300;

    // ── ARIA ──────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        return new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"]       = AriaRole,
            ["aria-modal"] = "true",
            ["aria-hidden"] = Open ? "false" : "true"
        };
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        OnKey("Escape", async () => { if (CloseOnEscape && Open) await CloseAsync(); });
        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (_isProcessingOverlay == 1) return;

        if (Open && !_wasOpen)
        {
            _wasOpen = true;
            await OnOpenCoreAsync();
        }
        else if (!Open && _wasOpen)
        {
            _wasOpen = false;
            await OnCloseCoreAsync(animate: true, ct: ComponentToken);
        }
    }

    // ── Core open/close ───────────────────────────────────────────────────────
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
        if (!IsDisposed) StateHasChanged();
    }

    // ИСПРАВЛЕНО: принимает CancellationToken напрямую, НЕ использует ComponentToken по умолчанию
    private async Task OnCloseCoreAsync(bool animate, CancellationToken ct)
    {
        if (animate && !ct.IsCancellationRequested)
        {
            IsAnimatingClose = true;
            if (!IsDisposed) StateHasChanged();

            using var animCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            animCts.CancelAfter(TimeSpan.FromMilliseconds(AnimationDurationMs + 100));
            try
            {
                await Task.Delay(AnimationDurationMs, animCts.Token);
            }
            catch (OperationCanceledException) { }

            IsAnimatingClose = false;
        }

        // FocusTrap — используем переданный ct
        if (TrapFocus && _focusTrapId != null)
        {
            try
            {
                if (!ct.IsCancellationRequested)
                    await FocusTrapService.DeactivateAsync(_focusTrapId);
            }
            catch { }
            _focusTrapId = null;
        }

        // ИСПРАВЛЕНО: unlockBodyScroll вызывается с явным ct (может быть cleanup token при Dispose)
        if (LockBodyScroll)
        {
            try
            {
                if (!ct.IsCancellationRequested)
                    await SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId);
            }
            catch { }
        }

        ZIndexService.Release(_zIndex);
        _zIndex = 0;

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync() => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    // ── Публичное API ─────────────────────────────────────────────────────────
    public async Task OpenAsync()
    {
        // ИСПРАВЛЕНО: Interlocked.CompareExchange вместо volatile bool
        if (Interlocked.CompareExchange(ref _isProcessingOverlay, 1, 0) == 1) return;
        if (Open) { Interlocked.Exchange(ref _isProcessingOverlay, 0); return; }
        try
        {
            Open = true;
            _wasOpen = true;
            await OpenChanged.InvokeAsync(true);
            await OnOpenCoreAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessingOverlay, 0);
        }
    }

    public async Task CloseAsync()
    {
        if (Interlocked.CompareExchange(ref _isProcessingOverlay, 1, 0) == 1) return;
        if (!Open) { Interlocked.Exchange(ref _isProcessingOverlay, 0); return; }
        try
        {
            Open = false;
            _wasOpen = false;
            await OpenChanged.InvokeAsync(false);
            await OnCloseCoreAsync(animate: true, ct: ComponentToken);
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessingOverlay, 0);
        }
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick) await CloseAsync();
    }

    // ── Dispose — ИСПРАВЛЕНО ──────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        if (Open || _focusTrapId != null || _zIndex > 0)
        {
            // ИСПРАВЛЕНО: создаём независимый CancellationToken для cleanup
            // который НЕ зависит от ComponentToken (уже отменён к этому моменту)
            using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await OnCloseCoreAsync(animate: false, ct: cleanupCts.Token);
        }
        await base.DisposeComponentAsync();
    }
}
