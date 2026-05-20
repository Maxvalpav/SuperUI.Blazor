using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Components;

/// <summary>
/// Base class for SuperUI input components.
/// Provides optional <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> integration:
/// when an ancestor supplies one (e.g. through <c>EditForm</c>), validation messages are surfaced and
/// the field participates in <c>OnFieldChanged</c>/<c>OnValidationStateChanged</c> notifications.
/// </summary>
/// <typeparam name="TValue">Type of the bound value.</typeparam>
public abstract class SgInputBase<TValue> : ComponentBase, IDisposable
{
    private bool _disposed;
    private bool _hasInitializedParameters;
    private EditContext? _previousEditContext;
    private Expression<Func<TValue?>>? _previousValueExpression;
    private FieldIdentifier _fieldIdentifier;

    /// <summary>
    /// Optional cascading <see cref="EditContext"/> supplied by an ancestor <c>EditForm</c>.
    /// </summary>
    [CascadingParameter] protected EditContext? CascadedEditContext { get; set; }

    /// <summary>The value bound to the input.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Callback invoked whenever <see cref="Value"/> changes.</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Lambda that identifies the bound model field, e.g. <c>() => model.Name</c>.</summary>
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>True while a parent <see cref="EditContext"/> reports validation errors for this field.</summary>
    protected bool HasValidationErrors
        => CascadedEditContext is not null
           && _fieldIdentifier.FieldName is not null
           && CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();

    /// <summary>Validation messages for the bound field, or empty when no <see cref="EditContext"/> is in use.</summary>
    protected IEnumerable<string> ValidationMessages
        => CascadedEditContext is not null && _fieldIdentifier.FieldName is not null
            ? CascadedEditContext.GetValidationMessages(_fieldIdentifier)
            : Array.Empty<string>();

    /// <summary>Identifier of the bound field within the cascaded <see cref="EditContext"/>, if any.</summary>
    protected FieldIdentifier FieldIdentifier => _fieldIdentifier;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!_hasInitializedParameters)
        {
            if (ValueExpression is not null)
            {
                _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            }
            _previousEditContext = CascadedEditContext;
            _previousValueExpression = ValueExpression;
            if (CascadedEditContext is not null)
            {
                CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
            }
            _hasInitializedParameters = true;
        }
        else if (!ReferenceEquals(_previousValueExpression, ValueExpression))
        {
            _fieldIdentifier = ValueExpression is null
                ? default
                : FieldIdentifier.Create(ValueExpression);
            _previousValueExpression = ValueExpression;
        }

        if (!ReferenceEquals(_previousEditContext, CascadedEditContext))
        {
            if (_previousEditContext is not null)
            {
                _previousEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
            }
            if (CascadedEditContext is not null)
            {
                CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
            }
            _previousEditContext = CascadedEditContext;
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }

    /// <summary>
    /// Sets <see cref="Value"/>, raises <see cref="ValueChanged"/>, and notifies the cascaded
    /// <see cref="EditContext"/> (if any) that the field changed.
    /// </summary>
    protected async Task SetValueAsync(TValue? next)
    {
        if (EqualityComparer<TValue?>.Default.Equals(next, Value)) return;
        Value = next;
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(next);
        }
        if (CascadedEditContext is not null && _fieldIdentifier.FieldName is not null)
        {
            CascadedEditContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (_disposed) return;
        _ = InvokeAsync(StateHasChanged).ContinueWith(
            t => Console.Error.WriteLine($"[SgInputBase] StateHasChanged failed: {t.Exception}"),
            System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_previousEditContext is not null)
        {
            _previousEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
            _previousEditContext = null;
        }
        GC.SuppressFinalize(this);
    }
}
