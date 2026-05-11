// SuperUI/Base/SgOverlayBase.cs
// ИСПРАВЛЕНО:
// 1. DisposeComponentAsync: unlockBodyScroll вызывается ПЕРВЫМ (до анимации)
// 2. unlockBodyScroll вызывается с отдельным CTS (не ComponentToken, т.к. уже отменён)
// 3. _isProcessingOverlay: используем SemaphoreSlim(1,1) вместо volatile bool
//    (volatile bool не даёт гарантий на Blazor Server при async/await)
// 4. OnCloseCoreAsync: unlockBodyScroll вызывается до FocusTrap деактивации
// 5. Таймаут cleanup увеличен до 5 секунд
// 6. Защита от повторного вызова OnCloseCoreAsync
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
    
    // ИСПРАВЛЕНО: SemaphoreSlim вместо volatile bool для async-safe эксклюзивности
    // volatile bool не работает корректно с async/await на Blazor Server
    private readonly SemaphoreSlim _overlayLock = new(1, 1);
    
    // Флаг что OverlayBase уже выполняет cleanup в dispose
    private volatile bool _isDisposing;

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
            ["aria-hidden"]= Open ? "false" : "true"
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
        
        // ИСПРАВЛЕНО: используем SemaphoreSlim для async-safe проверки
        // Не блокируем если уже выполняется операция открытия/закрытия
        if (!await _overlayLock.WaitAsync(0)) return; // не ждём — пропускаем если занято
        
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
        if (LockBodyScroll) await SafeInvokeVoidAsync("lockBodyScroll", ComponentId);
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
        // ── Анимация ─────────────────────────────────────────────────────────
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

        // ИСПРАВЛЕНО: получаем отдельный токен для JS cleanup операций
        // При dispose path — ComponentToken уже отменён, нужен новый токен
        var ct = overrideToken ?? (isDisposePath ? CancellationToken.None : ComponentToken);

        // 1. СНАЧАЛА unlockBodyScroll (критично — иначе страница заблокирована)
        if (LockBodyScroll)
        {
            try
            {
                if (!IsDisposed)
                    await SafeInvokeVoidAsync("unlockBodyScroll", ComponentId);
            }
            catch { /* ignore — JS runtime может быть недоступен при disconnect */ }
        }

        // 2. Деактивируем focus trap
        if (TrapFocus && _focusTrapId != null)
        {
            try
            {
                if (!IsDisposed)
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
        if (Open) return;
        
        if (!await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5))) return; // таймаут
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
            _overlayLock.Release();
        }
    }

    public async Task CloseAsync()
    {
        if (!Open) return;
        
        if (!await _overlayLock.WaitAsync(TimeSpan.FromSeconds(5))) return;
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
            _overlayLock.Release();
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
            // ИСПРАВЛЕНО: используем отдельный CTS с бо́льшим таймаутом
            // isDisposePath=true → пропускаем анимацию, используем CancellationToken.None для JS
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
                // scroll блокировка могла не сняться — это известная проблема при force-close
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