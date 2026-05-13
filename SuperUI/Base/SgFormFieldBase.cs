// SuperUI/Base/SgFormFieldBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ BUG-5: try/catch в OnParametersSet при ConvertBack
// ✅ UX-2: поддержка IAsyncSgConverter<TValue>
// ✅ НОВОЕ: FormFieldState enum — None/Dirty/Valid/Invalid
// ✅ НОВОЕ: ResetField() — сброс поля к начальному значению
//
// ⚠ Переименовано из SgFormBase<TValue> в SgFormFieldBase<TValue>
//   для устранения конфликта с SgFormBase<TModel> (контейнер формы).

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Converters;

namespace SuperUI.Base;

/// <summary>Состояние поля формы.</summary>
public enum FormFieldState { None, Dirty, Valid, Invalid }

/// <summary>Режим запуска валидации поля формы.</summary>
public enum SgFormValidationMode { OnChange, OnBlur, OnSubmit }

/// <summary>
/// Базовый класс для отдельного поля ввода в форме SuperUI.
/// </summary>
public abstract class SgFormFieldBase<TValue> : SgInteractiveBase
{
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }
    [Parameter] public ISgConverter<TValue>? Converter { get; set; }
    [Parameter] public IAsyncSgConverter<TValue>? AsyncConverter { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Hint { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public int? MaxLength { get; set; }
    [Parameter] public int? MinLength { get; set; }
    [Parameter] public SgFormValidationMode ValidationMode { get; set; } = SgFormValidationMode.OnChange;

    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore? _messageStore;
    private ISgConverter<TValue>? _effectiveConverter;
    private ISgConverter<TValue>? _previousConverter;
    private bool _editContextAttached;
    private TValue? _lastSyncedValue;
    private bool _isSettingValue;

    // НОВОЕ: состояние поля
    protected FormFieldState FieldState { get; private set; } = FormFieldState.None;

    protected EditContext? EditContext => _editContext;
    protected FieldIdentifier FieldId => _fieldIdentifier;

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

    protected IEnumerable<string> ValidationMessages =>
        _editContext?.GetValidationMessages(_fieldIdentifier) ??
        (ErrorText != null ? [ErrorText] : Enumerable.Empty<string>());

    protected string? ValidationCssClass => _editContext?.FieldCssClass(_fieldIdentifier);

    protected ISgConverter<TValue> EffectiveConverter
    {
        get
        {
            if (Converter is not null) return Converter;
            if (_effectiveConverter is not null) return _effectiveConverter;
            _effectiveConverter = SgConverterFactory.Get<TValue>() ??
                throw new InvalidOperationException(
                    $"Конвертер для '{typeof(TValue).FullName}' не найден. " +
                    $"Используйте Converter='...' или SgConverterFactory.Register<{typeof(TValue).Name}>().");
            return _effectiveConverter;
        }
    }

    protected string? CurrentText { get; private set; }
    protected string? ConvertError { get; private set; }

    // НОВОЕ: начальное значение для Reset
    private TValue? _initialValue;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _initialValue = Value;
    }

    // ── Методы изменения значения ───────────────────────────────────────────────
    protected async Task SetTextAsync(string? text)
    {
        CurrentText = text;
        FieldState = FormFieldState.Dirty;

        if (AsyncConverter is not null)
        {
            var (success, value, error) = await AsyncConverter.TryConvertAsync(text, ComponentToken);
            if (success)
            {
                ConvertError = null;
                await SetValueAsync(value);
            }
            else
            {
                ConvertError = error;
                FieldState = FormFieldState.Invalid;
                _editContext?.NotifyFieldChanged(_fieldIdentifier);
                await InvokeAsync(StateHasChanged);
            }
        }
        else if (EffectiveConverter.TryConvert(text, out var val, out var err))
        {
            ConvertError = null;
            await SetValueAsync(val);
        }
        else
        {
            ConvertError = err;
            FieldState = FormFieldState.Invalid;
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task SetValueAsync(TValue? value)
    {
        if (_isSettingValue) return;
        if (ValuesEqual(value, Value)) return;
        _isSettingValue = true;
        try
        {
            Value = value;
            _lastSyncedValue = value;
            // BUG-5 FIX: try/catch при ConvertBack
            try
            {
                CurrentText = AsyncConverter is not null
                    ? await AsyncConverter.ConvertBackAsync(value, ComponentToken)
                    : EffectiveConverter.ConvertBack(value);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[{Id}] ConvertBack failed for value {Val}", ComponentId, value);
                CurrentText = value?.ToString();
            }
            ConvertError = null;
            FieldState = HasError ? FormFieldState.Invalid : FormFieldState.Valid;
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await ValueChanged.InvokeAsync(value);
        }
        finally { _isSettingValue = false; }
    }

    private static bool ValuesEqual(TValue? a, TValue? b)
    {
        if (a is double da && b is double db)
            return (double.IsNaN(da) && double.IsNaN(db)) || da == db;
        if (a is float fa && b is float fb)
            return (float.IsNaN(fa) && float.IsNaN(fb)) || fa == fb;
        return EqualityComparer<TValue>.Default.Equals(a, b);
    }

    // НОВОЕ: сброс поля к начальному значению
    public async Task ResetFieldAsync()
    {
        FieldState = FormFieldState.None;
        ConvertError = null;
        await SetValueAsync(_initialValue);
    }

    // ── Валидация ───────────────────────────────────────────────────────────────
    public void AddValidationError(string message)
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Add(_fieldIdentifier, message);
        _editContext.NotifyValidationStateChanged();
        FieldState = FormFieldState.Invalid;
    }

    public void ClearValidationErrors()
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Clear(_fieldIdentifier);
        _editContext.NotifyValidationStateChanged();
        FieldState = HasError ? FormFieldState.Invalid : FormFieldState.Valid;
    }

    public void ValidateNow()
    {
        if (!IsDisposed) _editContext?.Validate();
    }

    // ── Focus / Blur ────────────────────────────────────────────────────────────
    protected new virtual async Task HandleBlurAsync(Microsoft.AspNetCore.Components.Web.FocusEventArgs e)
    {
        if (ValidationMode == SgFormValidationMode.OnBlur)
        {
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            FieldState = HasError ? FormFieldState.Invalid : FormFieldState.Valid;
        }
        await base.HandleBlurAsync(e);
    }

    // ── ARIA ────────────────────────────────────────────────────────────────────
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
        if (Hint != null) attrs["aria-describedby"] = $"{EffectiveId}-hint";
        if (HasError) attrs["aria-errormessage"] = $"{EffectiveId}-error";
        if (MaxLength.HasValue) attrs["aria-maxlength"] = MaxLength.Value.ToString();
        return attrs;
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────
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
        // BUG-5 FIX: try/catch при ConvertBack во время параметров
        if (!_isSettingValue && !EqualityComparer<TValue>.Default.Equals(Value, _lastSyncedValue))
        {
            _lastSyncedValue = Value;
            try
            {
                CurrentText = EffectiveConverter.ConvertBack(Value);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[{Id}] ConvertBack failed in OnParametersSet", ComponentId);
                CurrentText = Value?.ToString();
                ConvertError = ex.Message;
            }
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
        _editContext.NotifyValidationStateChanged();
        _messageStore = null;
        _editContextAttached = false;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;
        FieldState = HasError ? FormFieldState.Invalid : FormFieldState.Valid;
        _ = InvokeAsync(StateHasChanged);
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        DetachEditContext();
        await base.DisposeComponentAsync();
    }
}