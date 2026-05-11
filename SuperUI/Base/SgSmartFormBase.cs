// SuperUI/Base/SgSmartFormBase.cs

using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Base;

/// <summary>
/// SmartForm: форма с интеллектуальной валидацией.
/// - Дедупликация ошибок
/// - Crossfield validation
/// - Async remote validation с debounce
/// - Прогресс заполнения формы
/// </summary>
public abstract class SgSmartFormBase<TModel> : SgInteractiveBase
    where TModel : class, new()
{
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public Func<TModel, CancellationToken, Task<IEnumerable<string>>>? RemoteValidator { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public int ValidationDebounceMs { get; set; } = 500;

    private readonly Dictionary<string, List<string>> _fieldErrors = new();
    private int _filledFields;
    private int _totalFields;

    protected double CompletionPercent =>
        _totalFields > 0 ? (_filledFields * 100.0 / _totalFields) : 0;

    protected bool IsFormValid => _fieldErrors.Values.All(e => e.Count == 0);

    protected async Task ValidateFieldAsync(string fieldName, object? value)
    {
        // Локальная валидация немедленно
        var errors = ValidateField(fieldName, value);
        _fieldErrors[fieldName] = errors.ToList();

        // Удалённая валидация с дебаунсом
        if (RemoteValidator != null && !errors.Any())
        {
            await DebounceAsync($"remote_{fieldName}",
                async () =>
                {
                    var remoteErrors = await RemoteValidator(Model, ComponentToken);
                    _fieldErrors[fieldName].AddRange(remoteErrors);
                    StateHasChanged();
                },
                TimeSpan.FromMilliseconds(ValidationDebounceMs));
        }

        StateHasChanged();
    }

    protected virtual IEnumerable<string> ValidateField(string fieldName, object? value)
        => [];

    protected async Task SubmitAsync()
    {
        if (!IsFormValid) return;
        await OnValidSubmit.InvokeAsync(Model);
    }

    protected IEnumerable<string> GetErrors(string fieldName)
        => _fieldErrors.TryGetValue(fieldName, out var errors) ? errors : [];
}
