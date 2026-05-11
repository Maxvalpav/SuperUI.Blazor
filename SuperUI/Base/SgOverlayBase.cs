// SuperUI/Base/SgOverlayBase.cs
// ИСПРАВЛЕНО:
// 1. unlockBodyScroll вызывается ПЕРВЫМ в OnCloseCoreAsync (до FocusTrap, до анимации при dispose)
// 2. isDisposePath=true → пропускаем анимацию, используем CancellationToken.None для JS
// 3. _overlayLock: SemaphoreSlim вместо volatile bool (async-safe)
// 4. DisposeComponentAsync: отдельный cleanup CTS с таймаутом 5с
// 5. OpenAsync/CloseAsync защищены try/catch ObjectDisposedException для _overlayLock
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
    // volatile bool не работает корректно с async/await на Blazor Server
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
            ["role"]        = AriaRole,
            ["aria-modal"]  = "true",
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

        // Не блокируем если уже выполняется операция открытия/закрытия
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
        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            await FocusTrapService.ActivateAsync(_focusTrapId);
        }
        await OnOpenAsync();
        StateHasChanged();
    }

    private async Task OnCloseCoreAsync(
        bool animate = true,
        CancellationToken? overrideToken = null,
        bool isDisposePath = false)
    {
        // ── Анимация ──────────────────────────────────────────────────────────
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

        // Определяем токен для JS cleanup
        // При isDisposePath ComponentToken уже отменён → используем CancellationToken.None
        var ct = overrideToken ?? (isDisposePath ? CancellationToken.None : ComponentToken);

        // ИСПРАВЛЕНО: 1. СНАЧАЛА unlockBodyScroll — критично для UX
        // Даже если FocusTrap или cleanup займёт время → scroll уже разблокирован
        if (LockBodyScroll)
        {
            try
            {
                await SafeInvokeVoidAsync("unlockBodyScroll", ct, ComponentId);
            }
            catch
            {
                // ignore — JS runtime может быть недоступен при disconnect
            }
        }

        // 2. Деактивируем focus trap
        if (TrapFocus && _focusTrapId != null)
        {
            try
            {
                await FocusTrapService.DeactivateAsync(_focusTrapId);
            }
            catch { /* ignore during dispose */ }
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
        catch (ObjectDisposedException) { return; } // Dispose уже вызван

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
            try { _overlayLock.Release(); } catch (ObjectDisposedException) { }
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
            try { _overlayLock.Release(); } catch (ObjectDisposedException) { }
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
                // Таймаут истёк — принудительно сбрасываем состояние
                Logger.LogWarning("[{Id}] Overlay dispose cleanup timed out", ComponentId);
                _zIndex = 0;
                _focusTrapId = null;
            }
        }

        // Диспозим семафор ПОСЛЕ всех операций
        _overlayLock.Dispose();
        await base.DisposeComponentAsync();
    }
}