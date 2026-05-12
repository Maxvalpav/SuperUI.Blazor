// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНИЯ:
// ✅ EffectiveConverter: корректный lazy-init + InvalidOperationException с понятным сообщением
// ✅ SetValueAsync: NaN-safe сравнение, защита от рекурсии
// ✅ DetachEditContext: NotifyValidationStateChanged после очистки
// ✅ ValidateNow: IsDisposed check
// ✅ ClearValidationErrors: NotifyValidationStateChanged
// ✅ BuildAriaAttributes: убран нестандартный aria-placeholder
// УЛУЧШЕНИЯ:
// ✅ OnParametersSet: инвалидация _effectiveConverter при смене Converter
// ✅ SgFormValidationMode: режим валидации (OnChange | OnBlur | OnSubmit)
// ✅ OnBlur/OnFocus callbacks
// ✅ Текущий текст синхронизируется при смене Value извне

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Converters;

namespace SuperUI.Base;

/// <summary>
/// Режим валидации поля формы.
/// </summary>
public enum SgFormValidationMode
{
    /// <summary>Валидация при каждом изменении значения.</summary>
    OnChange,
    /// <summary>Валидация при потере фокуса.</summary>
    OnBlur,
    /// <summary>Валидация только при Submit формы.</summary>
    OnSubmit
}

/// <summary>
/// Базовый класс для компонентов-полей форм.
///
/// Иерархия: SgInteractiveBase → SgFormBase
///
/// Возможности:
/// - Двухсторонняя привязка Value / ValueChanged
/// - Интеграция с EditContext (валидация, CSS-классы)
/// - Конвертация string ↔ TValue через ISgConverter
/// - ARIA-атрибуты для accessibility
/// - Поддержка FluentValidation, DataAnnotations
/// </summary>
/// <typeparam name="TValue">Тип значения поля.</typeparam>
public abstract class SgFormBase<TValue> : SgInteractiveBase
{
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Текущее значение поля.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Callback изменения значения (two-way binding).</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Выражение для FieldIdentifier (валидация DataAnnotations).</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>Кастомный конвертер. Если null — используется SgConverterFactory.</summary>
    [Parameter] public ISgConverter<TValue>? Converter { get; set; }

    /// <summary>Метка поля.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Подсказка под полем.</summary>
    [Parameter] public string? Hint { get; set; }

    /// <summary>Принудительный текст ошибки (override валидации).</summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>Обязательное поле.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Placeholder-текст.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Максимальная длина строки.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Минимальная длина строки.</summary>
    [Parameter] public int? MinLength { get; set; }

    /// <summary>Режим валидации.</summary>
    [Parameter] public SgFormValidationMode ValidationMode { get; set; } = SgFormValidationMode.OnChange;

    /// <summary>Callback при получении фокуса.</summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>Callback при потере фокуса.</summary>
    [Parameter] public EventCallback OnBlur { get; set; }

    // ── Приватное состояние ───────────────────────────────────────────────────

    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore? _messageStore;
    private ISgConverter<TValue>? _effectiveConverter;
    private ISgConverter<TValue>? _previousConverter;
    private bool _editContextAttached;
    private TValue? _lastSyncedValue;
    private bool _isSettingValue;
    // M2 FIX: объект синхронизации для double-check locking
    private readonly object _converterLock = new();

    // ── Защищённые свойства ───────────────────────────────────────────────────

    /// <summary>Текущий EditContext (может быть null если вне EditForm).</summary>
    protected EditContext? EditContext => _editContext;

    /// <summary>FieldIdentifier для получения ошибок валидации.</summary>
    protected FieldIdentifier FieldId => _fieldIdentifier;

    /// <summary>Есть ошибка валидации или ConvertError.</summary>
    protected bool HasError
    {
        get
        {
            if (!string.IsNullOrEmpty(ErrorText)) return true;
            if (!string.IsNullOrEmpty(ConvertError)) return true;
            if (_editContext?.GetValidationMessages(_fieldIdentifier).Any() == true) return true;
            return false;
        }
    }

    /// <summary>Все сообщения валидации для поля.</summary>
    protected IEnumerable<string> ValidationMessages
        => _editContext?.GetValidationMessages(_fieldIdentifier)
           ?? (ErrorText != null ? [ErrorText] : Enumerable.Empty<string>());

    /// <summary>CSS-класс валидации от EditContext ("valid"/"invalid").</summary>
    protected string? ValidationCssClass => _editContext?.FieldCssClass(_fieldIdentifier);

    /// <summary>
    /// Эффективный конвертер (Converter ?? SgConverterFactory.Get).
    /// M2 FIX: double-check locking для thread-safety на Blazor Server.
    /// Lazy-init. Инвалидируется при смене параметра Converter.
    /// </summary>
    protected ISgConverter<TValue> EffectiveConverter
    {
        get
        {
            if (Converter is not null) return Converter;
            if (_effectiveConverter is not null)
                return _effectiveConverter;

            // M2 FIX: double-check locking
            lock (_converterLock)
            {
                if (_effectiveConverter is not null)
                    return _effectiveConverter;

                _effectiveConverter = SgConverterFactory.Get<TValue>()
                    ?? throw new InvalidOperationException(
                        $"Конвертер для типа '{typeof(TValue).FullName}' не найден. " +
                        $"Либо зарегистрируйте конвертер через SgConverterFactory.Register<{typeof(TValue).Name}>(), " +
                        $"либо укажите параметр Converter='...' явно.");
                return _effectiveConverter;
            }
        }
    }

    /// <summary>Текущий текст в поле ввода (строковое представление Value).</summary>
    protected string? CurrentText { get; private set; }

    /// <summary>Ошибка конвертации (null если ОК).</summary>
    protected string? ConvertError { get; private set; }

    // ── Методы изменения значения ─────────────────────────────────────────────

    /// <summary>
    /// Установить значение из строки пользовательского ввода.
    /// Конвертирует текст → TValue, обновляет EditContext.
    /// </summary>
    protected async Task SetTextAsync(string? text)
    {
        CurrentText = text;

        if (EffectiveConverter.TryConvert(text, out var value, out var error))
        {
            ConvertError = null;
            await SetValueAsync(value);
        }
        else
        {
            ConvertError = error;
            // Уведомляем EditContext об "грязном" состоянии даже при ошибке конвертации
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Установить типизированное значение.
    /// NaN-safe. Защита от рекурсии. Обновляет EditContext и вызывает ValueChanged.
    /// </summary>
    protected async Task SetValueAsync(TValue? value)
    {
        if (_isSettingValue)
        {
#if DEBUG
            Logger.LogDebug("[{Id}] SetValueAsync: рекурсивный вызов проигнорирован", ComponentId);
#endif
            return;
        }

        // Не вызываем ValueChanged если значение не изменилось (NaN-safe)
        if (ValuesEqual(value, Value)) return;

        _isSettingValue = true;
        try
        {
            Value = value;
            _lastSyncedValue = value;
            CurrentText = EffectiveConverter.ConvertBack(value);
            ConvertError = null;

            // EditContext ПЕРЕД событием (для корректного CSS-класса)
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await ValueChanged.InvokeAsync(value);
        }
        finally
        {
            _isSettingValue = false;
        }
    }

    /// <summary>
    /// Установить значение программно БЕЗ вызова ValueChanged.
    /// Используется для инициализации поля из внешнего источника (API, store).
    /// Обновляет CurrentText и сбрасывает ConvertError.
    /// </summary>
    protected void SetValueSilently(TValue? value)
    {
        _isSettingValue = true;
        try
        {
            Value = value;
            _lastSyncedValue = value;
            CurrentText = EffectiveConverter.ConvertBack(value);
            ConvertError = null;
        }
        finally
        {
            _isSettingValue = false;
        }
    }

    /// <summary>
    /// Установить текст программно БЕЗ конвертации и валидации.
    /// Используется для отображения текста из внешнего источника как есть.
    /// </summary>
    protected void SetTextSilently(string? text)
    {
        _isSettingValue = true;
        try
        {
            CurrentText = text;
            ConvertError = null;
        }
        finally
        {
            _isSettingValue = false;
        }
    }

    /// <summary>NaN-safe сравнение значений для предотвращения лишних обновлений.</summary>
    private static bool ValuesEqual(TValue? a, TValue? b)
    {
        if (a is double da && b is double db)
            return (double.IsNaN(da) && double.IsNaN(db)) || da == db;
        if (a is float fa && b is float fb)
            return (float.IsNaN(fa) && float.IsNaN(fb)) || fa == fb;
        return EqualityComparer<TValue>.Default.Equals(a, b);
    }

    // ── Валидация ─────────────────────────────────────────────────────────────

    /// <summary>Добавить ошибку валидации программно.</summary>
    public void AddValidationError(string message)
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Add(_fieldIdentifier, message);
        _editContext.NotifyValidationStateChanged();
    }

    /// <summary>Очистить программные ошибки валидации.</summary>
    public void ClearValidationErrors()
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Clear(_fieldIdentifier);
        _editContext.NotifyValidationStateChanged(); // ✅ уведомляем UI
    }

    /// <summary>Запустить валидацию вручную.</summary>
    public void ValidateNow()
    {
        if (!IsDisposed) _editContext?.Validate(); // ✅ IsDisposed check
    }

    // ── Blur/Focus ────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается при получении фокуса полем.
    /// Переопределите для кастомного поведения.
    /// </summary>
    protected virtual async Task HandleFocusAsync()
    {
        await OnFocus.InvokeAsync();
    }

    /// <summary>
    /// Вызывается при потере фокуса полем.
    /// При ValidationMode.OnBlur — запускает валидацию.
    /// </summary>
    protected virtual async Task HandleBlurAsync()
    {
        if (ValidationMode == SgFormValidationMode.OnBlur)
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
        await OnBlur.InvokeAsync();
    }

    // ── ARIA ──────────────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var baseAttrs = base.BuildAriaAttributes();
        bool needsExtra = Required || HasError || Label != null || MaxLength.HasValue || Hint != null;
        if (!needsExtra) return baseAttrs;

        var attrs = new Dictionary<string, object>(baseAttrs, StringComparer.Ordinal)
        {
            ["id"] = EffectiveId
        };

        if (Required) attrs["aria-required"] = "true";
        if (HasError) attrs["aria-invalid"] = "true";
        if (Label != null) attrs["aria-label"] = Label;

        // ✅ ИСПРАВЛЕНИЕ: aria-placeholder — нестандартный атрибут, убран
        // Hint → aria-describedby (стандартный ARIA-паттерн)
        if (Hint != null) attrs["aria-describedby"] = $"{EffectiveId}-hint";
        if (HasError) attrs["aria-errormessage"] = $"{EffectiveId}-error";
        if (MaxLength.HasValue) attrs["aria-maxlength"] = MaxLength.Value.ToString();

        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // M2 FIX: инвалидация под lock для консистентности
        if (!ReferenceEquals(Converter, _previousConverter))
        {
            lock (_converterLock)
            {
                _previousConverter = Converter;
                _effectiveConverter = null;
            }
        }

        // Обновляем FieldIdentifier при смене ValueExpression
        if (ValueExpression != null)
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        // Переключаем EditContext при каскадном изменении
        if (CascadedEditContext != _editContext)
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            AttachEditContext();
        }

        // Синхронизируем CurrentText если Value изменился извне (не через SetValueAsync)
        if (!_isSettingValue && !EqualityComparer<TValue>.Default.Equals(Value, _lastSyncedValue))
        {
            _lastSyncedValue = Value;
            CurrentText = EffectiveConverter.ConvertBack(Value);
            ConvertError = null;
        }
    }

    private void AttachEditContext()
    {
        if (_editContext is null || _editContextAttached) return;
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContextAttached = true;
    }

    private void DetachEditContext()
    {
        if (_editContext is null || !_editContextAttached) return;
        _editContext.OnValidationStateChanged -= OnValidationStateChanged;
        _messageStore?.Clear(_fieldIdentifier);
        _editContext.NotifyValidationStateChanged(); // ✅ уведомляем UI при откреплении
        _messageStore = null;
        _editContextAttached = false;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;
        _ = InvokeAsync(StateHasChanged);
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        DetachEditContext();
        await base.DisposeComponentAsync();
    }
}
