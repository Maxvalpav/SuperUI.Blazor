using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для оверлейных компонентов: Modal, Drawer, Popover, Tooltip, ContextMenu.
/// 
/// Обеспечивает:
/// - ZIndex управление через ZIndexService
/// - FocusTrap интеграция
/// - Portal рендеринг (teleport to body)
/// - Анимация открытия/закрытия
/// - Escape key обработка
/// - Body scroll lock
/// - ARIA dialog/alertdialog
/// </summary>
public abstract class SgOverlayBase : SgInteractiveBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IZIndexService ZIndexService { get; set; } = null!;
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = null!;

    // ── Параметры ─────────────────────────────────────────────────────────────

    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;
    [Parameter] public bool LockBodyScroll { get; set; } = true;
    [Parameter] public bool TrapFocus { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────

    private int _zIndex;
    private bool _wasOpen;
    private string? _focusTrapId;

    protected int EffectiveZIndex => _zIndex;
    protected bool IsAnimatingClose { get; private set; }

    // ── ARIA ──────────────────────────────────────────────────────────────────

    protected virtual string AriaRole => "dialog";

    protected override IReadOnlyDictionary<string, object?> BuildAriaAttributes()
    {
        var attrs = base.BuildAriaAttributes() as Dictionary<string, object?> ?? [];
        attrs["role"] = AriaRole;
        attrs["aria-modal"] = "true";
        attrs["aria-hidden"] = (!Open).ToString().ToLower();
        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Open && !_wasOpen)
        {
            await OnOpenAsync();
        }
        else if (!Open && _wasOpen)
        {
            await OnCloseAsync();
        }

        _wasOpen = Open;
    }

    protected override void OnInitialized()
    {
        // Регистрируем Escape
        OnKey("Escape", async () =>
        {
            if (CloseOnEscape && Open)
                await CloseAsync();
        });

        base.OnInitialized();
    }

    // ── Открытие / Закрытие ───────────────────────────────────────────────────

    protected virtual async Task OnOpenAsync()
    {
        // Получить z-index из сервиса
        _zIndex = ZIndexService.GetNext();

        // Заблокировать скролл body
        if (LockBodyScroll)
            await SafeInvokeVoidAsync("lockBodyScroll", ComponentId);

        // Установить FocusTrap
        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            await FocusTrapService.ActivateAsync(_focusTrapId);
        }

        StateHasChanged();
    }

    protected virtual async Task OnCloseAsync()
    {
        IsAnimatingClose = true;
        StateHasChanged();

        // Ждём анимацию
        await Task.Delay(AnimationDurationMs, ComponentToken);

        IsAnimatingClose = false;

        // Освободить FocusTrap
        if (TrapFocus && _focusTrapId != null)
        {
            await FocusTrapService.DeactivateAsync(_focusTrapId);
            _focusTrapId = null;
        }

        // Разблокировать скролл
        if (LockBodyScroll)
            await SafeInvokeVoidAsync("unlockBodyScroll", ComponentId);

        // Вернуть z-index
        ZIndexService.Release(_zIndex);
        _zIndex = 0;

        StateHasChanged();
    }

    /// <summary>Длительность анимации в мс. Переопределить для каждого оверлея.</summary>
    protected virtual int AnimationDurationMs => 300;

    public async Task OpenAsync()
    {
        if (Open) return;
        Open = true;
        await OpenChanged.InvokeAsync(true);
        await OnOpenAsync();
    }

    public async Task CloseAsync()
    {
        if (!Open) return;
        Open = false;
        await OpenChanged.InvokeAsync(false);
        await OnCloseAsync();
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick)
            await CloseAsync();
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        if (Open)
        {
            await OnCloseAsync();
        }
        await base.DisposeComponentAsync();
    }
}
