using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Components;

/// <summary>
/// Helper that wires a SuperUI input into an ancestor <see cref="EditContext"/>.
/// Use from components that cannot inherit <see cref="SgInputBase{TValue}"/>
/// (e.g. components with their own value generics or non-nullable value semantics).
/// </summary>
internal sealed class SgEditContextBinder<TValue> : IDisposable
{
    private readonly Action _stateChanged;
    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private Expression<Func<TValue?>>? _valueExpression;
    private bool _initialized;

    public SgEditContextBinder(Action stateChanged)
    {
        _stateChanged = stateChanged;
    }

    public FieldIdentifier FieldIdentifier => _fieldIdentifier;

    public bool HasValidationErrors
        => _editContext is not null
           && _fieldIdentifier.FieldName is not null
           && _editContext.GetValidationMessages(_fieldIdentifier).Any();

    public IEnumerable<string> ValidationMessages
        => _editContext is not null && _fieldIdentifier.FieldName is not null
            ? _editContext.GetValidationMessages(_fieldIdentifier)
            : Array.Empty<string>();

    /// <summary>
    /// Call from <c>OnParametersSet</c> with the current cascaded <see cref="EditContext"/>
    /// and the <c>ValueExpression</c> parameter. Manages subscription lifecycle.
    /// </summary>
    public void Update(EditContext? editContext, Expression<Func<TValue?>>? valueExpression)
    {
        if (!_initialized || !ReferenceEquals(_valueExpression, valueExpression))
        {
            _fieldIdentifier = valueExpression is null
                ? default
                : FieldIdentifier.Create(valueExpression);
            _valueExpression = valueExpression;
        }

        if (!ReferenceEquals(_editContext, editContext))
        {
            if (_editContext is not null)
            {
                _editContext.OnValidationStateChanged -= OnValidationStateChanged;
            }
            _editContext = editContext;
            if (_editContext is not null)
            {
                _editContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// Notifies the bound <see cref="EditContext"/> that the field changed.
    /// </summary>
    public void NotifyFieldChanged()
    {
        if (_editContext is not null && _fieldIdentifier.FieldName is not null)
        {
            _editContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        => _stateChanged();

    public void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;
            _editContext = null;
        }
    }
}
