// Файл: Components/Base/SgFormBase.cs
// Зависимости: SgInteractiveBase (уровень 2), Converter<T>, EditContext

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Converters;
using SuperUI.State;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 3A: Базовый класс для компонентов форм (inputs, selects, date pickers...).
/// 
/// РЕАЛИЗУЕТ:
/// - Value / ValueChanged / ValueExpression — стандартный Blazor binding
/// - EditContext интеграция (validation messages, field state)
/// - Converter[TValue, string] — двусторонняя конвертация
/// - ValidationMessageStore управление
/// - FieldIdentifier для точечной валидации
/// </summary>
/// <typeparam name="TValue">Тип значения поля формы.</typeparam>
public abstract class SgFormBase<TValue> : SgInteractiveBase
{
    // ── Cascading параметры формы ─────────────────────────────────────────────

    [CascadingParameter] protected EditContext? CascadedEditContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Текущее значение поля.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Callback при изменении значения (двусторонний binding).</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Expression для FieldIdentifier (валидация).</summary>
    [Parameter] public System.Linq.Expressions.Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Метка поля.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Placeholder.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Только чтение (не disabled, но нельзя изменить).</summary>
    [Parameter] public bool Readonly { get; set; }

    /// <summary>Обязательное поле.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>
    /// Конвертер значения в строку и обратно.
    /// Если не задан — используется DefaultConverter.
    /// </summary>
    [Parameter] public Converter<TValue>? Converter { get; set; }

    /// <summary>Формат для конвертации (например, "dd.MM.yyyy" для DateTime).</summary>
    [Parameter] public string? Format { get; set; }

    // ── ParameterState регистрация ────────────────────────────────────────────

    protected readonly ParameterState<TValue?> _valueState;

    protected SgFormBase()
    {
        using var scope = CreateRegisterScope();
        _valueState = scope.RegisterParameter<TValue?>(nameof(Value))
            .WithParameter(() => Value)
            .WithEventCallback(() => ValueChanged)
            .WithChangeHandler(OnValueChangedAsync);
    }

    // ── EditContext интеграция ────────────────────────────────────────────────

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private FieldIdentifier _fieldIdentifier;
    private bool _editContextInitialized;

    protected EditContext? CurrentEditContext => _editContext ?? CascadedEditContext;
    protected FieldIdentifier FieldId => _fieldIdentifier;

    /// <summary>Есть ли ошибки валидации для этого поля.</summary>
    public bool HasValidationErrors =>
        _editContext?.GetValidationMessages(_fieldIdentifier).Any() == true;

    /// <summary>Сообщения об ошибках валидации.</summary>
    public IEnumerable<string> ValidationMessages =>
        _editContext?.GetValidationMessages(_fieldIdentifier) ?? Enumerable.Empty<string>();

    // ── Конвертация ───────────────────────────────────────────────────────────

    private Converter<TValue>? _effectiveConverter;

    protected Converter<TValue> EffectiveConverter
        => _effectiveConverter ??= Converter ?? CreateDefaultConverter();

    /// <summary>Создать конвертер по умолчанию. Наследники переопределяют для специфической логики.</summary>
    protected virtual Converter<TValue> CreateDefaultConverter()
        => new CultureAwareConverter<TValue>(CurrentCulture);

    /// <summary>Текущее значение как строка.</summary>
    protected string? ValueAsString
    {
        get => EffectiveConverter.Convert(_valueState.Value);
        set
        {
            var converted = EffectiveConverter.ConvertBack(value);
            _ = _valueState.SetValueAsync(converted);
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnComponentInitialized()
    {
        base.OnComponentInitialized();
        InitializeEditContext();
    }

    protected override void OnComponentParametersSet()
    {
        base.OnComponentParametersSet();

        // Пересоздаём конвертер если изменился Converter или культура
        _effectiveConverter = null; // lazy recreation

        // Переинициализируем EditContext если изменился (редко, но возможно)
        if (CascadedEditContext != _editContext && !_editContextInitialized)
            InitializeEditContext();
    }

    private void InitializeEditContext()
    {
        // Отписываемся от старого
        if (_editContext is not null)
        {
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;
            _editContext.OnFieldChanged -= OnFieldChanged;
        }

        _editContext = CascadedEditContext;

        if (_editContext is not null)
        {
            if (ValueExpression is not null)
                _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

            _messageStore = new ValidationMessageStore(_editContext);

            // Подписываемся через EventSubscriptionManager (авто-отписка)
            Subscribe(
                () =>
                {
                    _editContext.OnValidationStateChanged += OnValidationStateChanged;
                    _editContext.OnFieldChanged += OnFieldChanged;
                },
                () =>
                {
                    _editContext.OnValidationStateChanged -= OnValidationStateChanged;
                    _editContext.OnFieldChanged -= OnFieldChanged;
                });

            _editContextInitialized = true;
        }
    }

    // ── EditContext events ────────────────────────────────────────────────────

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        => _ = RequestStateUpdateAsync(); // перерисовать для обновления ошибок

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (e.FieldIdentifier.Equals(_fieldIdentifier))
            _ = RequestStateUpdateAsync();
    }

    // ── Value management ──────────────────────────────────────────────────────

    /// <summary>Уведомить EditContext об изменении поля.</summary>
    protected void NotifyFieldChanged()
    {
        if (_editContext is not null && _fieldIdentifier != default)
            _editContext.NotifyFieldChanged(_fieldIdentifier);
    }

    /// <summary>Добавить ошибку валидации программно.</summary>
    protected void AddValidationError(string message)
    {
        if (_messageStore is not null && _fieldIdentifier != default)
        {
            _messageStore.Add(_fieldIdentifier, message);
            _editContext?.NotifyValidationStateChanged();
        }
    }

    /// <summary>Очистить ошибки валидации.</summary>
    protected void ClearValidationErrors()
    {
        if (_messageStore is not null)
        {
            _messageStore.Clear(_fieldIdentifier);
            _editContext?.NotifyValidationStateChanged();
        }
    }

    // ── Виртуальные хуки ──────────────────────────────────────────────────────

    /// <summary>Вызывается при изменении Value через ParameterState.</summary>
    protected virtual ValueTask OnValueChangedAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Установить новое значение программно.
    /// Уведомляет EditContext и ValueChanged callback.
    /// </summary>
    protected async ValueTask SetValueAsync(TValue? newValue)
    {
        await _valueState.SetValueAsync(newValue);
        NotifyFieldChanged();
        ClearValidationErrors();
    }

    // ── ARIA для форм ─────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> GetAriaAttributes()
    {
        var attrs = (Dictionary<string, object?>)base.GetAriaAttributes();

        if (Required) attrs["aria-required"] = "true";
        if (Readonly) attrs["aria-readonly"] = "true";
        if (HasValidationErrors) attrs["aria-invalid"] = "true";

        var labelId = $"{ComponentId}-label";
        if (Label is not null) attrs["aria-labelledby"] = labelId;

        var errorId = $"{ComponentId}-error";
        if (HasValidationErrors) attrs["aria-describedby"] = errorId;

        return attrs;
    }
}
