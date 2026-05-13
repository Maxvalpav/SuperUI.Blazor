// SuperUI/Base/SgOverlayBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ BUG-2: Channel<OverlayCommand> вместо WaitAsync(0) — Open/Close не теряется
// ✅ UX-4: ReturnFocusOnClose — WCAG 2.1 SC 2.4.3
// ✅ PERF-5: Volatile.Read вместо Interlocked.CompareExchange
// ✅ AnimationDurationMs: кеш из JS только при первом рендере

using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

public abstract class SgOverlayBase : SgInteractiveBase
{
    [Inject] protected IZIndexService ZIndexService { get; set; } = null!;
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = null!;

    // ── Параметры ───────────────────────────────────────────────────────────────
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;
    [Parameter] public bool LockBodyScroll { get; set; } = true;
    [Parameter] public bool TrapFocus { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Description { get; set; }

    // UX-4: возврат фокуса на trigger при закрытии
    [Parameter] public string? ReturnFocusToId { get; set; }

    // ── Состояние ───────────────────────────────────────────────────────────────
    private int _zIndex;
    private bool _wasOpen;
    private string? _focusTrapId;
    private int _isDisposingInt;
    private int _animationDurationMs = 300;

    // BUG-2 FIX: Channel для очереди команд Open/Close
    private readonly Channel<bool> _commandChannel =
        Channel.CreateBounded<bool>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    private Task? _commandProcessorTask;

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }

    // ── Виртуальные свойства ────────────────────────────────────────────────────
    protected virtual string AriaRole => "dialog";

    // PERF-5 FIX: Volatile.Read — быстрее Interlocked.CompareExchange
    protected virtual int AnimationDurationMs =>
        Math.Max(0, Volatile.Read(ref _animationDurationMs));

    protected virtual bool HasAriaModal => true;
    protected virtual int ZIndexBase => IZIndexService.ModalBase;

    // ── ARIA ────────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"] = AriaRole,
            ["aria-hidden"] = Open ? "false" : "true"
        };
        if (HasAriaModal) attrs["aria-modal"] = "true";
        if (!string.IsNullOrWhiteSpace(Title))
            attrs["aria-labelledby"] = $"{ComponentId}-title";
        if (!string.IsNullOrWhiteSpace(Description))
            attrs["aria-describedby"] = $"{ComponentId}-description";
        return attrs;
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        OnKey("Escape", async () =>
        {
            if (CloseOnEscape && Open) await CloseAsync();
        });
        // Запускаем processor команд
        _commandProcessorTask = ProcessCommandsAsync();
    }

    // BUG-2 FIX: параметры только пишут в Channel, processor обрабатывает
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1) return;

        if (Open != _wasOpen)
            await _commandChannel.Writer.WriteAsync(Open, ComponentToken);
    }

    private async Task ProcessCommandsAsync()
    {
        try
        {
            await foreach (var shouldOpen in _commandChannel.Reader.ReadAllAsync(ComponentToken))
            {
                if (IsDisposed) break;
                if (shouldOpen && !_wasOpen)
                {
                    _wasOpen = true;
                    await OnOpenCoreAsync();
                }
                else if (!shouldOpen && _wasOpen)
                {
                    _wasOpen = false;
                    await OnCloseCoreAsync(animate: true);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (!IsPrerendering)
        {
            var ms = await SafeInvokeAsync<int, string>("getAnimationDuration", ComponentId);
            if (ms > 0)
                Volatile.Write(ref _animationDurationMs, ms);
        }
    }

    // ── Внутренние методы ───────────────────────────────────────────────────────
    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.Allocate(ZIndexBase);
        if (LockBodyScroll)
            await SafeInvokeVoidAsync<string>("lockBodyScroll", ComponentId);
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
        if (!IsDisposed) StateHasChanged();
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
                using var animCts = CancellationTokenSource.CreateLinkedTokenSource(
                    overrideToken ?? ComponentToken);
                animCts.CancelAfter(TimeSpan.FromMilliseconds(AnimationDurationMs + 50));
                await Task.Delay(AnimationDurationMs, animCts.Token);
            }
            catch (OperationCanceledException) { }
            IsAnimatingClose = false;
        }

        if (LockBodyScroll)
        {
            try { await SafeInvokeVoidAsync<string>("unlockBodyScroll", ComponentId); }
            catch { }
        }

        if (TrapFocus && _focusTrapId != null)
        {
            try { await FocusTrapService.DeactivateAsync(_focusTrapId); }
            catch { }
            _focusTrapId = null;
        }

        // UX-4: возврат фокуса на trigger-элемент (WCAG 2.1)
        if (!isDisposePath && !string.IsNullOrWhiteSpace(ReturnFocusToId))
        {
            try
            {
                await SafeInvokeVoidAsync<string>("returnFocus", ReturnFocusToId);
            }
            catch { }
        }

        try { ZIndexService.Release(_zIndex); }
        finally { _zIndex = 0; }

        await OnCloseAsync();
        if (!IsDisposed) StateHasChanged();
    }

    protected virtual Task OnOpenAsync() => Task.CompletedTask;
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    // ── Публичные методы ────────────────────────────────────────────────────────
    public async Task OpenAsync()
    {
        if (Open || IsDisposed ||
            Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1) return;
        Open = true;
        await OpenChanged.InvokeAsync(true);
        await _commandChannel.Writer.WriteAsync(true, ComponentToken);
    }

    public async Task CloseAsync()
    {
        if (!Open || IsDisposed ||
            Interlocked.CompareExchange(ref _isDisposingInt, 0, 0) == 1) return;
        Open = false;
        await OpenChanged.InvokeAsync(false);
        await _commandChannel.Writer.WriteAsync(false, ComponentToken);
    }

    public Task ToggleAsync() => Open ? CloseAsync() : OpenAsync();

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (IsDisposed) return;
        if (CloseOnBackdropClick) await CloseAsync();
    }

    // ── Dispose ─────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        if (Interlocked.Exchange(ref _isDisposingInt, 1) == 1) return;

        _commandChannel.Writer.TryComplete();

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

        if (_commandProcessorTask is not null)
        {
            try { await _commandProcessorTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch { }
        }

        await base.DisposeComponentAsync();
    }
}
