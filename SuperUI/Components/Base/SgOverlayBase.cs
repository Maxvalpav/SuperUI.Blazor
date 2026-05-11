// Файл: Components/Base/SgOverlayBase.cs
// Зависимости: SgInteractiveBase (уровень 2), ZIndexService, FocusTrapService

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;
using SuperUI.State;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 3B: Базовый класс для overlay-компонентов (Modal, Drawer, Popover, Tooltip...).
/// 
/// РЕАЛИЗУЕТ:
/// - Open/Close управление с анимацией
/// - ZIndex stack (правильный порядок оверлеев)
/// - FocusTrap активация/деактивация
/// - Escape key handler
/// - Backdrop управление
/// - Portal/Teleport rendering
/// - ARIA dialog/alertdialog
/// </summary>
public abstract class SgOverlayBase : SgInteractiveBase
{
    // ── Инжекции ──────────────────────────────────────────────────────────────

    [Inject] protected IZIndexService ZIndexService { get; set; } = default!;

    // ── ParameterState для Open ───────────────────────────────────────────────

    protected readonly ParameterState<bool> _openState;

    protected SgOverlayBase()
    {
        using var scope = CreateRegisterScope();
        _openState = scope.RegisterParameter<bool>(nameof(Open))
            .WithParameter(() => Open)
            .WithEventCallback(() => OpenChanged)
            .WithChangeHandler(OnOpenChangedAsync);
    }

    // ── Параметры ─────────────────────────────────────────────────────────────

    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Закрыть при клике на backdrop.</summary>
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Закрыть при нажатии Escape.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Показывать backdrop.</summary>
    [Parameter] public bool HasBackdrop { get; set; } = true;

    /// <summary>Callback перед открытием (можно отменить).</summary>
    [Parameter] public EventCallback<OverlayOpeningEventArgs> OnOpening { get; set; }

    /// <summary>Callback после открытия.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>Callback перед закрытием (можно отменить).</summary>
    [Parameter] public EventCallback<OverlayClosingEventArgs> OnClosing { get; set; }

    /// <summary>Callback после закрытия.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    /// <summary>ARIA role: dialog или alertdialog.</summary>
    [Parameter] public string AriaRole { get; set; } = "dialog";

    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? AriaLabelledBy { get; set; }
    [Parameter] public string? AriaDescribedBy { get; set; }

    // ── Состояние ─────────────────────────────────────────────────────────────

    private int _zIndex;
    private bool _isVisible; // для анимации (open != visible)
    private bool _isAnimating;

    protected bool IsVisible => _isVisible;
    protected bool IsAnimating => _isAnimating;
    protected int CurrentZIndex => _zIndex;

    // ── Open/Close методы ─────────────────────────────────────────────────────

    /// <summary>Открыть overlay.</summary>
    public async Task OpenAsync()
    {
        if (_openState.Value) return;

        var args = new OverlayOpeningEventArgs();
        if (OnOpening.HasDelegate)
        {
            await OnOpening.InvokeAsync(args);
            if (args.Cancel) return;
        }

        await _openState.SetValueAsync(true);
    }

    /// <summary>Закрыть overlay.</summary>
    public async Task CloseAsync()
    {
        if (!_openState.Value) return;

        var args = new OverlayClosingEventArgs();
        if (OnClosing.HasDelegate)
        {
            await OnClosing.InvokeAsync(args);
            if (args.Cancel) return;
        }

        await _openState.SetValueAsync(false);
    }

    /// <summary>Переключить состояние.</summary>
    public Task ToggleAsync() => _openState.Value ? CloseAsync() : OpenAsync();

    // ── Open state change ─────────────────────────────────────────────────────

    private async ValueTask OnOpenChangedAsync()
    {
        if (_openState.Value)
            await HandleOpenAsync();
        else
            await HandleCloseAsync();
    }

    private async ValueTask HandleOpenAsync()
    {
        // 1. Получить z-index из стека
        _zIndex = ZIndexService.Acquire(ComponentId);

        // 2. Показать (начать анимацию)
        _isVisible = true;
        _isAnimating = true;
        await RequestStateUpdateAsync();

        // 3. Активировать FocusTrap
        await ActivateFocusTrapAsync();

        // 4. Зарегистрировать Escape
        if (CloseOnEscape)
            RegisterKeyboardShortcut("Escape", OnEscapeKeyAsync, preventDefault: true);

        // 5. Callback
        if (OnOpened.HasDelegate)
            await OnOpened.InvokeAsync();

        // 6. Конец анимации
        _isAnimating = false;
        await RequestStateUpdateAsync();
    }

    private async ValueTask HandleCloseAsync()
    {
        // 1. Начать анимацию закрытия
        _isAnimating = true;
        await RequestStateUpdateAsync();

        // 2. Деактивировать FocusTrap
        await DeactivateFocusTrapAsync();

        // 3. Освободить z-index
        ZIndexService.Release(ComponentId);

        // 4. Подождать анимацию
        await Task.Delay(GetCloseAnimationDuration());

        // 5. Скрыть
        _isVisible = false;
        _isAnimating = false;
        await RequestStateUpdateAsync();

        // 6. Callback
        if (OnClosed.HasDelegate)
            await OnClosed.InvokeAsync();
    }

    /// <summary>Длительность анимации закрытия. Наследники переопределяют.</summary>
    protected virtual int GetCloseAnimationDuration() => 300;

    private async ValueTask OnEscapeKeyAsync(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs _)
        => await CloseAsync();

    // ── Backdrop click ────────────────────────────────────────────────────────

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick)
            await CloseAsync();
    }

    // ── ARIA для overlays ─────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> GetAriaAttributes()
    {
        var attrs = (Dictionary<string, object?>)base.GetAriaAttributes();

        attrs["role"] = AriaRole;
        attrs["aria-modal"] = "true";

        if (_openState.Value) attrs["aria-expanded"] = "true";
        else attrs["aria-hidden"] = "true";

        if (AriaLabel is not null) attrs["aria-label"] = AriaLabel;
        if (AriaLabelledBy is not null) attrs["aria-labelledby"] = AriaLabelledBy;
        if (AriaDescribedBy is not null) attrs["aria-describedby"] = AriaDescribedBy;

        return attrs;
    }

    protected override string GetComponentPrefix() => "overlay";

    // ── Dispose ───────────────────────────────────────────────────────────────

    protected override async ValueTask OnComponentDisposeAsync()
    {
        if (_openState.Value)
            ZIndexService.Release(ComponentId);

        await base.OnComponentDisposeAsync();
    }
}

// ── Event Args ────────────────────────────────────────────────────────────────

public sealed class OverlayOpeningEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}

public sealed class OverlayClosingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}
