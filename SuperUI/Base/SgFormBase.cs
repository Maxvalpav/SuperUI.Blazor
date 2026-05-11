// SuperUI/Base/SgFormBase.cs
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Base.Converters;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы.
///
/// ИСПРАВЛЕНИЯ:
/// 1. IDisposable удалён (Blazor не вызывает Dispose — только DisposeAsync)
/// 2. OnValidationStateChanged: проверка IsDisposed перед InvokeAsync
/// 3. DetachEditContext: идемпотентная операция (guard flag)
/// 4. SetValueAsync: обновление CurrentText атомарно с Value
/// 5. BuildAriaAttributes: кэширование из базового класса
/// 6. CurrentText: синхронизация только при реальном изменении Value
/// </summary>
public abstract class SgFormBase<TValue> : SgInteractiveBase
{
    // ── Инъекции / Каскадные ──────────────────────────────────────────────────
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// Пользовательский конвертер.
    [Parameter] public ISgConverter<TValue>? Converter { get; set; }

    [Parameter] public string? Label    { get; set; }
    [Parameter] public string? Hint     { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public bool    Required { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public int?    MaxLength { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private EditContext?             _editContext;
    private FieldIdentifier          _fieldIdentifier;
    private ValidationMessageStore?  _messageStore;
    private ISgConverter<TValue>?    _effectiveConverter;
    private bool                     _editContextAttached;
    private TValue?                  _lastSyncedValue; // для отслеживания изменений Value

    protected EditContext?     EditContext => _editContext;
    protected FieldIdentifier  FieldId    => _fieldIdentifier;

    // ── Вычисляемые ──────────────────────────────────────────────────────────
    protected bool HasError =>
        !string.IsNullOrEmpty(ErrorText) ||
        !string.IsNullOrEmpty(ConvertError) ||
        (_editContext != null && _editContext.GetValidationMessages(_fieldIdentifier).Any());

    protected IEnumerable<string> ValidationMessages =>
        _editContext?.GetValidationMessages(_fieldIdentifier) ??
        (ErrorText != null ? [ErrorText] : []);

    protected string? ValidationCssClass =>
        _editContext?.FieldCssClass(_fieldIdentifier);

    // ── Конвертер ─────────────────────────────────────────────────────────────
    protected ISgConverter<TValue> EffectiveConverter =>
        Converter ?? (_effectiveConverter ??= SgConverterFactory.Get<TValue>());

    // ── Text/Value синхронизация ──────────────────────────────────────────────
    protected string? CurrentText  { get; private set; }
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

        var previous = Value;
        Value = value;
        _lastSyncedValue = value;
        CurrentText = EffectiveConverter.ConvertBack(value);

        await ValueChanged.InvokeAsync(value);

        // Уведомить EditContext
        _editContext?.NotifyFieldChanged(_fieldIdentifier);
    }

    // ── ARIA для формы ────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var baseAttrs = base.BuildAriaAttributes();

        // Только если есть что добавить — создаём новый dict
        bool needsExtra = Required || HasError || Label != null || Placeholder != null;
        if (!needsExtra) return baseAttrs;

        var attrs = new Dictionary<string, object>(baseAttrs, StringComparer.Ordinal)
        {
            ["id"] = EffectiveId
        };

        if (Required)    attrs["aria-required"]    = "true";
        if (HasError)    attrs["aria-invalid"]      = "true";
        if (Label != null) attrs["aria-label"]      = Label;
        if (Placeholder != null) attrs["aria-placeholder"] = Placeholder;

        // Связать с hint/error элементами
        if (Hint != null)      attrs["aria-describedby"] = $"{EffectiveId}-hint";
        if (HasError)          attrs["aria-errormessage"] = $"{EffectiveId}-error";

        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Обновить FieldIdentifier
        if (ValueExpression != null)
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        // Подписаться на EditContext
        if (CascadedEditContext != _editContext)
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            AttachEditContext();
        }

        // ИСПРАВЛЕНО: синхронизировать CurrentText только при реальном изменении Value
        if (!EqualityComparer<TValue>.Default.Equals(Value, _lastSyncedValue))
        {
            _lastSyncedValue = Value;
            CurrentText = EffectiveConverter.ConvertBack(Value);
            ConvertError = null; // сброс ошибки конвертации при внешнем изменении
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
        _messageStore = null;
        _editContextAttached = false;
    }

    /// <summary>
    /// ИСПРАВЛЕНО: проверка IsDisposed перед InvokeAsync.
    /// </summary>
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (!IsDisposed)
            InvokeAsync(StateHasChanged);
    }

    // ── Dispose — ИСПРАВЛЕНО ─────────────────────────────────────────────────
    /// <summary>
    /// ИСПРАВЛЕНО: IDisposable убран (Blazor не вызывает Dispose, только DisposeAsync).
    /// DetachEditContext — идемпотентен благодаря _editContextAttached флагу.
    /// </summary>
    protected override async ValueTask DisposeComponentAsync()
    {
        DetachEditContext();
        await base.DisposeComponentAsync();
    }
}
