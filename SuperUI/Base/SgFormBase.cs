using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Utilities;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы.
/// 
/// Обеспечивает:
/// - EditContext полная интеграция
/// - SgConverter<T,string> — двунаправленная конвертация
/// - Валидация с CSSClass
/// - FieldIdentifier авто-вычисление
/// - Error boundary для компонента
/// - Auto-unsubscribe от EditContext событий
/// </summary>
public abstract class SgFormBase<TValue> : SgInteractiveBase, IDisposable
{
    // ── Инъекции / Каскадные ──────────────────────────────────────────────────
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Для @bind-Value поддержки Expression.</summary>
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Пользовательский конвертер. Если null — используется SgConverterFactory.</summary>
    [Parameter] public ISgConverter<TValue>? Converter { get; set; }

    /// <summary>Текст метки поля.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Текст подсказки.</summary>
    [Parameter] public string? Hint { get; set; }

    /// <summary>Текст ошибки (ручная установка, перекрывает EditContext).</summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>Обязательное поле.</summary>
    [Parameter] public bool Required { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────

    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore? _messageStore;
    private ISgConverter<TValue>? _effectiveConverter;

    protected EditContext? EditContext => _editContext;
    protected FieldIdentifier FieldId => _fieldIdentifier;

    // ── Вычисляемые ──────────────────────────────────────────────────────────

    protected bool HasError => !string.IsNullOrEmpty(ErrorText)
        || (_editContext != null && _editContext.GetValidationMessages(_fieldIdentifier).Any());

    protected IEnumerable<string> ValidationMessages
        => _editContext?.GetValidationMessages(_fieldIdentifier)
           ?? (ErrorText != null ? [ErrorText] : []);

    protected string? ValidationCssClass
        => _editContext?.FieldCssClass(_fieldIdentifier);

    // ── Конвертер ─────────────────────────────────────────────────────────────

    protected ISgConverter<TValue> EffectiveConverter
        => Converter ?? (_effectiveConverter ??= SgConverterFactory.Get<TValue>());

    // ── Text <-> Value синхронизация ──────────────────────────────────────────

    protected string? CurrentText { get; private set; }
    protected string? ConvertError { get; private set; }

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
        }
    }

    protected async Task SetValueAsync(TValue? value)
    {
        if (EqualityComparer<TValue>.Default.Equals(value, Value)) return;

        Value = value;
        CurrentText = EffectiveConverter.ConvertBack(value);

        await ValueChanged.InvokeAsync(value);

        // Уведомить EditContext об изменении поля
        if (_editContext != null)
        {
            _editContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    // ── ARIA для формы ────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> BuildAriaAttributes()
    {
        var attrs = base.BuildAriaAttributes() as Dictionary<string, object?> ?? [];

        if (Required)
            attrs["aria-required"] = "true";

        if (HasError)
            attrs["aria-invalid"] = "true";

        if (Label != null)
            attrs["aria-label"] = Label;

        // Связать с label элементом
        attrs["id"] = EffectiveId;

        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Обновить FieldIdentifier при изменении ValueExpression
        if (ValueExpression != null)
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        // Подписаться на EditContext
        if (CascadedEditContext != _editContext)
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            AttachEditContext();
        }

        // Синхронизировать CurrentText с Value
        CurrentText = EffectiveConverter.ConvertBack(Value);
    }

    private void AttachEditContext()
    {
        if (_editContext is null) return;

        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _messageStore = new ValidationMessageStore(_editContext);
    }

    private void DetachEditContext()
    {
        if (_editContext is null) return;
        _editContext.OnValidationStateChanged -= OnValidationStateChanged;
        _messageStore = null;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        DetachEditContext();
        await base.DisposeComponentAsync();
    }

    void IDisposable.Dispose()
    {
        DetachEditContext();
    }
}
