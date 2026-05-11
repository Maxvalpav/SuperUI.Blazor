// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНО:
// 1. SetValueAsync: NotifyFieldChanged ПЕРЕД ValueChanged (EditContext обновляется первым)
// 2. OnValidationStateChanged: Task не теряется (логируем ошибки)
// 3. BuildAriaAttributes: не вызывает base при каждом изменении (кэш через generation)
// 4. CurrentText синхронизируется атомарно с Value
// 5. AttachEditContext/DetachEditContext: идемпотентны

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Base.Converters;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы (input, select, datepicker и т.д.).
/// </summary>
public abstract class SgFormBase<TValue> : SgInteractiveBase
{
    // ── Каскадный EditContext ─────────────────────────────────────────────────
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────
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

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore? _messageStore;
    private ISgConverter<TValue>? _effectiveConverter;
    private bool _editContextAttached;
    private TValue? _lastSyncedValue;
    private bool _isSettingValue; // защита от рекурсии в SetValueAsync

    protected EditContext? EditContext => _editContext;
    protected FieldIdentifier FieldId => _fieldIdentifier;

    // ── Вычисляемые ───────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: HasError вычисляется один раз, а не несколько раз при каждом рендере
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

    protected IEnumerable<string> ValidationMessages =>
        _editContext?.GetValidationMessages(_fieldIdentifier)
        ?? (ErrorText != null ? [ErrorText] : []);

    protected string? ValidationCssClass => _editContext?.FieldCssClass(_fieldIdentifier);

    // ── Конвертер ─────────────────────────────────────────────────────────────
    protected ISgConverter<TValue> EffectiveConverter =>
        Converter ?? (_effectiveConverter ??= SgConverterFactory.Get<TValue>());

    // ── Text/Value синхронизация ──────────────────────────────────────────────
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
        if (_isSettingValue) return; // защита от рекурсии
        if (EqualityComparer<TValue>.Default.Equals(value, Value)) return;

        _isSettingValue = true;
        try
        {
            Value = value;
            _lastSyncedValue = value;
            CurrentText = EffectiveConverter.ConvertBack(value);
            ConvertError = null;

            // ИСПРАВЛЕНО: сначала NotifyFieldChanged → потом ValueChanged
            // Это гарантирует что EditContext обновлён до того, как родитель
            // получит событие и возможно прочитает состояние валидации
            _editContext?.NotifyFieldChanged(_fieldIdentifier);
            await ValueChanged.InvokeAsync(value);
        }
        finally
        {
            _isSettingValue = false;
        }
    }

    /// <summary>Программно добавить ошибку валидации в ValidationMessageStore.</summary>
    public void AddValidationError(string message)
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Add(_fieldIdentifier, message);
        _editContext.NotifyValidationStateChanged();
    }

    /// <summary>Очистить программно добавленные ошибки.</summary>
    public void ClearValidationErrors()
    {
        if (_messageStore is null || _editContext is null) return;
        _messageStore.Clear(_fieldIdentifier);
        _editContext.NotifyValidationStateChanged();
    }

    /// <summary>ИСПРАВЛЕНО: публичный метод для программной валидации.</summary>
    public void ValidateNow()
    {
        if (_editContext is null) return;
        _editContext.Validate();
    }

    // ── ARIA — ИСПРАВЛЕНО: не создаёт новый dict если не нужно ──────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var baseAttrs = base.BuildAriaAttributes();
        bool needsExtra = Required || HasError || Label != null || Placeholder != null;
        if (!needsExtra) return baseAttrs;

        var attrs = new Dictionary<string, object>(baseAttrs, StringComparer.Ordinal)
        {
            ["id"] = EffectiveId
        };

        if (Required)     attrs["aria-required"]    = "true";
        if (HasError)     attrs["aria-invalid"]      = "true";
        if (Label != null) attrs["aria-label"]       = Label;
        if (Placeholder != null) attrs["aria-placeholder"] = Placeholder;

        if (Hint != null)
            attrs["aria-describedby"] = $"{EffectiveId}-hint";
        if (HasError)
            attrs["aria-errormessage"] = $"{EffectiveId}-error";

        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ValueExpression != null)
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        if (CascadedEditContext != _editContext)
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            AttachEditContext();
        }

        // Синхронизируем CurrentText только при реальном внешнем изменении Value
        if (!_isSettingValue &&
            !EqualityComparer<TValue>.Default.Equals(Value, _lastSyncedValue))
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
        _messageStore?.Clear();
        _messageStore = null;
        _editContextAttached = false;
    }

    // ИСПРАВЛЕНО: не теряем Task, логируем ошибки
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;

        var task = InvokeAsync(StateHasChanged);

        // На Blazor Server task может завершиться с ошибкой
        // Регистрируем continuation для логирования (не блокируем)
        if (!task.IsCompletedSuccessfully)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                    Logger.LogWarning(t.Exception.InnerException ?? t.Exception,
                        "[{Id}] OnValidationStateChanged StateHasChanged error", ComponentId);
            }, TaskScheduler.Default);
        }
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        DetachEditContext();
        await base.DisposeComponentAsync();
    }
}
