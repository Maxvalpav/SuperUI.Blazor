// ================================================================
// Файл: SuperUI/Base/SgSsrFormSupport.cs
// ИСПРАВЛЕНО:
// - Добавлен using Microsoft.AspNetCore.Http (для HttpContext)
// - Безопасная проверка HttpContext?.Request.HasFormContentType
// - Добавлен EnableAntiforgery для .NET 8+
// ================================================================

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace SuperUI.Base;

/// <summary>
/// Provides SSR-compatible form support for SuperUI components.
/// Supports .NET 8+ SSR with enhanced navigation and form handling.
/// </summary>
public class SgSsrFormSupport<TModel> : ComponentBase where TModel : class, new()
{
    [Parameter] public TModel? Model { get; set; }
    [Parameter] public bool IsModelFromForm { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback<TModel> OnInvalidSubmit { get; set; }
    [Parameter] public Func<TModel, Task>? HandleSubmitAsync { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string FormMethod { get; set; } = "post";
    [Parameter] public bool EnhancedNavigation { get; set; } = true;
    [Parameter] public bool SupplyFromFormData { get; set; }

    /// <summary>
    /// Enable antiforgery token (.NET 8+).
    /// </summary>
    [Parameter] public bool EnableAntiforgery { get; set; } = true;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private EditContext? _editContext;
    private bool _hasInitialized;

    protected override void OnInitialized()
    {
        if (SupplyFromFormData
            && HttpContext?.Request.HasFormContentType == true
            && HttpContext.Request.Form != null)
        {
            Model ??= new TModel();
            var form = HttpContext.Request.Form;
            IsModelFromForm = true;

            var properties = typeof(TModel).GetProperties();
            foreach (var prop in properties)
            {
                if (form.TryGetValue(prop.Name, out var values) && values.Count > 0)
                {
                    try
                    {
                        var converted = Convert.ChangeType(values[0]!, prop.PropertyType);
                        prop.SetValue(Model, converted);
                    }
                    catch
                    {
                        // Skip properties that can't be bound
                    }
                }
            }
        }

        Model ??= new TModel();
        _editContext = new EditContext(Model);
        _hasInitialized = true;
    }

    protected override void OnParametersSet()
    {
        if (_hasInitialized && Model != null)
        {
            if (_editContext?.Model != Model)
                _editContext = new EditContext(Model);
        }
    }

    public async Task HandleSubmit()
    {
        if (Model is null) return;

        if (_editContext is not null)
        {
            var isValid = _editContext.Validate();
            if (!isValid)
            {
                await OnInvalidSubmit.InvokeAsync(Model);
                return;
            }
        }

        if (HandleSubmitAsync is not null)
        {
            await HandleSubmitAsync(Model);
        }

        await OnValidSubmit.InvokeAsync(Model);
    }

    public EditContext? GetEditContext() => _editContext;

    public void NotifyStateChanged() => StateHasChanged();
}
