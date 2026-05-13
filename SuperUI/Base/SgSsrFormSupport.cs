// SuperUI/Base/SgSsrFormSupport.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Base;

/// <summary>
/// Provides SSR-compatible form support for SuperUI components.
/// Bridges the gap between traditional form POST handling and
/// Blazor's EditForm/EditContext model in SSR scenarios.
/// 
/// Supports .NET 8+ SSR with enhanced navigation and form handling.
/// </summary>
public class SgSsrFormSupport<TModel> : ComponentBase where TModel : class, new()
{
    [Parameter] public TModel? Model { get; set; }
    [Parameter] public bool IsModelFromForm { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnInvalidSubmit { get; set; }
    [Parameter] public Func<TModel, Task>? HandleSubmitAsync { get; set; }
    [Parameter] public RenderFragment<TModel>? ChildContent { get; set; }
    [Parameter] public string FormMethod { get; set; } = "post";
    [Parameter] public bool EnhancedNavigation { get; set; } = true;
    [Parameter] public bool SupplyFromFormData { get; set; }

    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    private EditContext? _editContext;
    private bool _hasInitialized;

    protected override void OnInitialized()
    {
        // Initialize Model if not provided via form data
        if (SupplyFromFormData && HttpContext?.Request.HasFormContentType == true)
        {
            Model ??= new TModel();
            var form = HttpContext.Request.Form;
            IsModelFromForm = true;

            // Simple model binding from form data
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

    /// <summary>Handle form submission (SSR-compatible).</summary>
    public async Task HandleSubmit()
    {
        if (Model is null) return;

        if (_editContext is not null)
        {
            var isValid = _editContext.Validate();
            if (!isValid)
            {
                await OnInvalidSubmit.InvokeAsync();
                return;
            }
        }

        if (HandleSubmitAsync is not null)
        {
            await HandleSubmitAsync(Model);
        }

        await OnValidSubmit.InvokeAsync(Model);
    }

    /// <summary>Expose the EditContext to child components.</summary>
    public EditContext? GetEditContext() => _editContext;

    /// <summary>Notify the form that state has changed (triggers re-render).</summary>
    public void NotifyStateChanged() => StateHasChanged();
}
