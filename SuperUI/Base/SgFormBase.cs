using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Converters;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы.
/// Уровень 4: ... → SgInteractiveBase → SgFormBase
///
/// ИСПРАВЛЕНО:
/// 1. DetachEditContext — вызывает NotifyValidationStateChanged() после Clear() (UI обновляется).
/// 2. NaN-safe ValuesEqual для double/float.
/// 3. ValidateNow — IsDisposed check.
/// 4. aria-placeholder удалён (нестандартный ARIA-атрибут).
/// </summary>
public abstract class SgFormBase<TValue> : SgInteractiveBase
{
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }
    [Parameter] public ISgConverter<TValue>? Converter { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Hint { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public int? MaxLength { get; set; }

    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore? _messageStore;
    private ISgConverter<TValue>? _effectiveConverter;
    private ISgConverter<TValue>? _previousConverter;
    private bool _editContextAttached;
    private TValue? _lastSyncedValue;
    private bool _isSettingValue;

    protected EditContext? EditContext => _editContext;
    protected FieldIdentifier FieldId => _fieldIdentifier;

    protected bool HasError
    {
        get
        {
            if (!string.IsNullOrEmpty(ErrorText)) return true;
            if (!string.IsNullOrEmpty(ConvertError)) return true;
            if (_editContext != null && _editContext.GetValidationMessages(_fieldIdentifier).Any()) return true;
            return false;
        }
    }

    protected IEnumerable<string> ValidationMessages
        => _editContext?.GetValidationMessages(_fieldIdentifier)
           ?? (ErrorText != null ? [ErrorText] : []);

    protected string? ValidationCssClass => _editContext?.FieldCssClass(_fieldIdentifier);

    protected ISgConverter<TValue> EffectiveConverter
        => Converter ?? (_effectiveConverter ??= SgConverterFactory.Get<TValue>());

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
        if (_isSettingValue)
        {
#if DEBUG
            Logger.LogDebug("[{Id}] SetValueAsync: recursive call ignored", ComponentId);
#endif
            return;
        }

        // ИСПРАВЛЕНО: NaN-safe сравнение для double/float
        if (ValuesEqual(value, Value)) return;

        _isSettingValue = true;
        try
        {
            Value = value;
            _lastSyncedValue = value;
            CurrentText = EffectiveConverter.ConvertBack(value);
            ConvertError = null;

            // EditContext должен обновиться ДО события (родитель может читать валидацию)
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await ValueChanged.InvokeAsync(value);
        }
        finally
        {
            _isSettingValue = false;
        }
    }

    /// <summary>
    /// ИСПРАВЛЕНО: NaN-safe equality для double/float.
    /// double.NaN != double.NaN, поэтому нужна специальная проверка.
    /// </summary>
    private static bool ValuesEqual(TValue? a, TValue? b)
    {
        if (a is double da && b is double db)
            return (double.IsNaN(da) && double.IsNaN(db)) || da == db;
        if (a is float fa && b is float fb)
            return (float.IsNaN(fa) && float.IsNaN(fb)) || fa == fb;
        return EqualityComparer<TValue>.Default.Equals(a, b);
    }

    public void AddValidationError(string message)
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Add(_fieldIdentifier, message);
        _editContext.NotifyValidationStateChanged();
    }

    public void ClearValidationErrors()
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Clear(_fieldIdentifier);
        // ИСПРАВЛЕНО: уведомляем UI после очистки
        _editContext.NotifyValidationStateChanged();
    }

    // ИСПРАВЛЕНО: проверка IsDisposed
    public void ValidateNow()
    {
        if (!IsDisposed) _editContext?.Validate();
    }

    // ── ARIA ──────────────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var baseAttrs = base.BuildAriaAttributes();
        bool needsExtra = Required || HasError || Label != null
                          || MaxLength.HasValue || Hint != null;
        if (!needsExtra) return baseAttrs;

        var attrs = new Dictionary<string, object>(baseAttrs, StringComparer.Ordinal)
        {
            ["id"] = EffectiveId
        };

        if (Required) attrs["aria-required"] = "true";
        if (HasError) attrs["aria-invalid"] = "true";
        if (Label != null) attrs["aria-label"] = Label;
        // ИСПРАВЛЕНО: aria-placeholder нестандартный — убран
        if (Hint != null) attrs["aria-describedby"] = $"{EffectiveId}-hint";
        if (HasError) attrs["aria-errormessage"] = $"{EffectiveId}-error";
        if (MaxLength.HasValue) attrs["aria-maxlength"] = MaxLength.Value.ToString();

        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!ReferenceEquals(Converter, _previousConverter))
        {
            _previousConverter = Converter;
            _effectiveConverter = null;
        }

        if (ValueExpression != null)
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        if (CascadedEditContext != _editContext)
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            AttachEditContext();
        }

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
        // ИСПРАВЛЕНО: уведомляем UI после очистки
        _editContext.NotifyValidationStateChanged();
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
