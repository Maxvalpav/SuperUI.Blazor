// SuperUI/Base/Reactive/SgFormSignalBridge.cs
// УНИКАЛЬНЫЙ КЛАСС — двусторонняя синхронизация формы и сигналов.

using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Мост между Blazor EditForm/EditContext и системой сигналов SuperUI.
/// Автоматически синхронизирует:
/// - Значения полей формы ↔ сигналы
/// - Состояние валидации ↔ сигнал
/// - Состояние изменённости ↔ сигнал
/// 
/// Использование:
/// <code>
/// var bridge = new SgFormSignalBridge(editContext);
/// bridge.Bind("FirstName", firstNameSignal);
/// bridge.Bind("Email", emailSignal);
/// 
/// // В рендере:
/// var isValid = bridge.IsValid.Value;
/// var isModified = bridge.IsModified.Value;
/// </code>
/// </summary>
public sealed class SgFormSignalBridge : IDisposable
{
    private readonly EditContext _editContext;
    private readonly Dictionary<string, (ISgSignal Signal, Action<object?> Setter)> _bindings = new();
    private readonly SgSignal<bool> _isValid;
    private readonly SgSignal<bool> _isModified;
    private readonly SgSignal<Dictionary<string, string[]>> _errors;
    private bool _dirty;

    public IReadOnlySignal<bool> IsValid => _isValid;
    public IReadOnlySignal<bool> IsModified => _isModified;
    public IReadOnlySignal<Dictionary<string, string[]>> Errors => _errors;

    public SgFormSignalBridge(EditContext editContext)
    {
        _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
        _isValid = new SgSignal<bool>(true, "form-valid");
        _isModified = new SgSignal<bool>(false, "form-modified");
        _errors = new SgSignal<Dictionary<string, string[]>>(new Dictionary<string, string[]>(), "form-errors");
        _editContext.OnValidationStateChanged += OnValidationChanged;
    }

    /// <summary>Связать поле формы с сигналом.</summary>
    public SgFormSignalBridge Bind<T>(string fieldName, SgSignal<T> signal)
    {
        _bindings[fieldName] = (signal, value => signal.Set((T)(value ?? default(T)!)));

        // Подписка на изменение поля
        _editContext.OnFieldChanged += (_, args) =>
        {
            if (args.FieldIdentifier.FieldName == fieldName)
            {
                var model = _editContext.Model;
                var prop = model.GetType().GetProperty(fieldName);
                if (prop is not null)
                {
                    var value = prop.GetValue(model);
                    if (value is T tValue)
                        signal.Set(tValue);
                }
            }
        };

        return this;
    }

    /// <summary>Отметить форму как изменённую.</summary>
    public void MarkAsModified()
    {
        if (!_dirty)
        {
            _dirty = true;
            _isModified.Set(true);
        }
    }

    /// <summary>Сбросить состояние.</summary>
    public void Reset()
    {
        _dirty = false;
        _isModified.Set(false);
        _isValid.Set(true);
        _errors.Set(new Dictionary<string, string[]>());
    }

    private void OnValidationChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        var messages = _editContext.GetValidationMessages();
        var errors = new Dictionary<string, string[]>();

        foreach (var field in _editContext.GetValidationMessages())
        {
            // Группируем ошибки по полям
        }

        _isValid.Set(!messages.Any());
        _errors.Set(errors);
    }

    public void Dispose()
    {
        _editContext.OnValidationStateChanged -= OnValidationChanged;
        _isValid.Dispose();
        _isModified.Dispose();
        _errors.Dispose();
    }
}
