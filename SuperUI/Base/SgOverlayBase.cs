// SuperUI/Base/SgOverlayBase.cs
//
// УЛУЧШЕНИЯ:
// 1. HandleBackdropClickAsync: добавлен IsDisposed check.
// 2. OnCloseCoreAsync: анимация использует linkedCts с ComponentToken.
// 3. OpenAsync/CloseAsync: таймаут WaitAsync логируется.
// 4. ToggleAsync: новый метод для toggle Open/Close.
// 5. BuildAriaAttributes: добавлен aria-labelledby если есть Title параметр.
// 6. AnimationDurationMs: защита от отрицательных значений.
//
// ДОРАБОТКИ:
// 7. ZIndexBase — virtual property для переопределения базового z-index в подклассах.
// 8. OnOpenCoreAsync: Allocate(ZIndexBase) вместо GetNext().

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

public abstract class SgOverlayBase : SgInteractiveBase
{
    [Inject] protected IZIndexService     ZIndexService     { get; set; } = null!;
    [Inject] protected IFocusTrapService  FocusTrapService  { get; set; } = null!;

    [Parameter] public bool            Open                  { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged       { get; set; }
    [Parameter] public bool            CloseOnEscape         { get; set; } = true;
    [Parameter] public bool            CloseOnBackdropClick  { get; set; } = true;
    [Parameter] public bool            LockBodyScroll        { get; set; } = true;
    [Parameter] public bool            TrapFocus             { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent          { get; set; }
    [Parameter] public string?         Title                 { get; set; }

    private int     _zIndex;
    private bool    _wasOpen;
    private string? _focusTrapId;
    private int     _isDisposingInt;
    private volatile int _animationDurationMs = 300;
    private readonly SemaphoreSlim _overlayLock = new(1, 1);

    protected int  EffectiveZIndex    => _zIndex;
    protected bool IsAnimatingClose   { get; private set; }
    protected virtual string AriaRole => "dialog";

    // ДОРАБОТКА: защита от отрицательных значений
    protected virtual int AnimationDurationMs =>
        Math.Max(0, _animationDurationMs);

    protected virtual bool HasAriaModal => true;

    /// <summary>Базовый z-index для этого типа overlay. Переопределите в подклассе.</summary>
    protected virtual int ZIndexBase => Services.ZIndexService.ModalBase;

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"]        = AriaRole,
            ["aria-hidden"] = Open ? "false" : "true"
        };
        if (HasAriaModal) attrs["aria-modal"] = "true";
        // УЛУЧШЕНО: aria-labelledby если есть Title
        if (!string.IsNullOrWhiteSpace(Title))
            attrs["aria-labelledby"] = $"{ComponentId}-title";
        return attrs;
    }

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
        // ДОРАБОТКА: Allocate(ZIndexBase) вместо GetNext()
        _zIndex = ZIndexService.Allocate(ZIndexBase);
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
                // УЛУЧШЕНО: linked token с ComponentToken
                using var animCts = CancellationTokenSource.CreateLinkedTokenSource(
                    overrideToken ?? ComponentToken);
                animCts.CancelAfter(TimeSpan.FromMilliseconds(AnimationDurationMs + 50));
                await Task.Delay(AnimationDurationMs, animCts.Token);
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

        try { ZIndexService.Release(_zIndex); }
        finally { _zIndex = 0; }

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync()  => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

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
            Open    = true;
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
            Open    = false;
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

    /// <summary>Переключить состояние открытия overlay.</summary>
    public Task ToggleAsync() => Open ? CloseAsync() : OpenAsync();

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        // УЛУЧШЕНО: IsDisposed check
        if (IsDisposed) return;
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
                _zIndex      = 0;
                _focusTrapId = null;
            }
        }

        _overlayLock.Dispose();
        await base.DisposeComponentAsync();
    }
}
