// Файл: Components/Base/SgInteractiveBase.cs
// Зависимости: SgComponentBase (уровень 1), KeyboardHandlerService, MouseHandlerService

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;
using SuperUI.Utilities;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 2: Базовый класс для интерактивных компонентов.
/// Добавляет: keyboard/mouse handling, debounce, throttle, focus management.
/// 
/// Наследуется компонентами: SgButton, SgTextBox, SgSelect, SgModal, SgMenu, etc.
/// </summary>
public abstract class SgInteractiveBase : SgComponentBase
{
    // ── Инжекции ──────────────────────────────────────────────────────────────

    [Inject] protected IKeyboardHandlerService KeyboardService { get; set; } = default!;
    [Inject] protected IMouseHandlerService MouseService { get; set; } = default!;
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = default!;

    // ── Дебаунсер / Throttler ─────────────────────────────────────────────────

    /// <summary>Debouncer для откладывания обработки ввода.</summary>
    protected SgDebouncer? _debouncer;

    /// <summary>Throttler для ограничения частоты событий.</summary>
    protected SgThrottler? _throttler;

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Задержка debounce в мс (0 = нет debounce).</summary>
    [Parameter] public int DebounceDelay { get; set; } = 0;

    /// <summary>Интервал throttle в мс (0 = нет throttle).</summary>
    [Parameter] public int ThrottleInterval { get; set; } = 0;

    /// <summary>Поддерживает ли компонент получение фокуса.</summary>
    [Parameter] public bool Focusable { get; set; } = true;

    /// <summary>Tab index элемента.</summary>
    [Parameter] public int TabIndex { get; set; } = 0;

    // ── События ───────────────────────────────────────────────────────────────

    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    // ── Зарегистрированные keyboard shortcuts ─────────────────────────────────

    private readonly List<KeyboardShortcutRegistration> _keyboardRegistrations = new();
    private readonly List<MouseEventRegistration> _mouseRegistrations = new();

    // ── Refs для FocusTrap ────────────────────────────────────────────────────

    protected ElementReference _rootRef;
    private string? _focusTrapId;
    private bool _focusTrapActive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnComponentInitialized()
    {
        base.OnComponentInitialized();

        // Инициализируем debouncer/throttler только если нужны
        if (DebounceDelay > 0)
            _debouncer = RegisterDisposable(new SgDebouncer(DebounceDelay));

        if (ThrottleInterval > 0)
            _throttler = RegisterDisposable(new SgThrottler(ThrottleInterval));
    }

    protected override void OnComponentParametersSet()
    {
        base.OnComponentParametersSet();

        // Пересоздаём если delay изменился
        if (DebounceDelay > 0 && (_debouncer is null))
            _debouncer = RegisterDisposable(new SgDebouncer(DebounceDelay));
    }

    // ── Keyboard registration ─────────────────────────────────────────────────

    /// <summary>
    /// Зарегистрировать глобальный keyboard shortcut.
    /// Автоматически снимается при dispose компонента.
    /// 
    /// ИСПОЛЬЗОВАНИЕ (в OnAfterRenderAsync):
    /// RegisterKeyboardShortcut("Escape", OnEscapePressed);
    /// </summary>
    protected void RegisterKeyboardShortcut(
        string key,
        Func<KeyboardEventArgs, ValueTask> handler,
        KeyboardModifiers modifiers = KeyboardModifiers.None,
        bool preventDefault = false)
    {
        var registration = new KeyboardShortcutRegistration(
            ComponentId, key, modifiers, handler, preventDefault);

        _keyboardRegistrations.Add(registration);
        Subscribe(
            () => KeyboardService.Register(registration),
            () => KeyboardService.Unregister(registration));
    }

    /// <summary>Зарегистрировать глобальный mouse event handler.</summary>
    protected void RegisterMouseHandler(
        MouseEventType eventType,
        Func<MouseEventArgs, ValueTask> handler)
    {
        var registration = new MouseEventRegistration(ComponentId, eventType, handler);
        _mouseRegistrations.Add(registration);
        Subscribe(
            () => MouseService.Register(registration),
            () => MouseService.Unregister(registration));
    }

    // ── FocusTrap ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Активировать FocusTrap внутри компонента.
    /// Фокус не выйдет за пределы _rootRef пока trap активен.
    /// </summary>
    protected async ValueTask ActivateFocusTrapAsync()
    {
        if (_focusTrapActive) return;
        _focusTrapId = await FocusTrapService.ActivateAsync(_rootRef, ComponentId);
        _focusTrapActive = true;
    }

    /// <summary>Деактивировать FocusTrap.</summary>
    protected async ValueTask DeactivateFocusTrapAsync()
    {
        if (!_focusTrapActive || _focusTrapId is null) return;
        await FocusTrapService.DeactivateAsync(_focusTrapId);
        _focusTrapActive = false;
        _focusTrapId = null;
    }

    // ── Event handlers с debounce/throttle ───────────────────────────────────

    /// <summary>
    /// Обработать событие с debounce.
    /// Если debounce не настроен — выполнить сразу.
    /// </summary>
    protected ValueTask HandleWithDebounceAsync(
        Func<CancellationToken, ValueTask> action,
        CancellationToken ct = default)
    {
        if (_debouncer is not null)
        {
            _debouncer.Debounce(action, ct);
            return ValueTask.CompletedTask;
        }
        return action(ct);
    }

    /// <summary>Обработать событие с throttle.</summary>
    protected ValueTask HandleWithThrottleAsync(Func<ValueTask> action)
    {
        if (_throttler is not null)
            return _throttler.ThrottleAsync(action).AsValueTask();
        return action();
    }

    // ── Focus management ──────────────────────────────────────────────────────

    /// <summary>Программно установить фокус на компонент.</summary>
    public virtual ValueTask FocusAsync()
        => JSInvokeVoidAsync("SuperUI.focus", default, ComponentId);

    /// <summary>Убрать фокус с компонента.</summary>
    public virtual ValueTask BlurAsync()
        => JSInvokeVoidAsync("SuperUI.blur", default, ComponentId);

    // ── ARIA расширение ────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> GetAriaAttributes()
    {
        var attrs = (Dictionary<string, object?>)base.GetAriaAttributes();
        if (Focusable && !Disabled)
            attrs["tabindex"] = TabIndex.ToString();
        else if (Disabled)
            attrs["tabindex"] = "-1";
        return attrs;
    }

    // ── Dispose расширение ────────────────────────────────────────────────────

    protected override async ValueTask OnComponentDisposeAsync()
    {
        if (_focusTrapActive)
            await DeactivateFocusTrapAsync();

        await base.OnComponentDisposeAsync();
    }
}

// ── Вспомогательные типы ──────────────────────────────────────────────────────

[Flags]
public enum KeyboardModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Meta = 8
}

public enum MouseEventType { Click, ContextMenu, DblClick, MouseDown, MouseUp, MouseMove }

public sealed record KeyboardShortcutRegistration(
    string ComponentId,
    string Key,
    KeyboardModifiers Modifiers,
    Func<KeyboardEventArgs, ValueTask> Handler,
    bool PreventDefault);

public sealed record MouseEventRegistration(
    string ComponentId,
    MouseEventType EventType,
    Func<MouseEventArgs, ValueTask> Handler);
