// SuperUI/Base/SgComponentErrorBoundary.razor.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SuperUI.Base.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Base;

public partial class SgComponentErrorBoundary : ComponentBase, IDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }
    [Parameter] public string? ComponentName { get; set; }
    [Parameter] public bool ShowErrorMessage { get; set; } = true;
    [Parameter] public int MaxErrorCount { get; set; } = 3;

    [Inject] private ComponentDiagnostics? Diagnostics { get; set; }
    [Inject] private ILogger<SgComponentErrorBoundary> Logger { get; set; } = NullLogger<SgComponentErrorBoundary>.Instance;

    private Exception? _currentError;
    private int _errorCount;
    private bool _isDisposed;
    private bool _isRecovering;

    protected bool HasError => _currentError != null;
    protected bool TooManyErrors => _errorCount >= MaxErrorCount;

    protected override void OnParametersSet()
    {
        if (_errorCount >= MaxErrorCount)
        {
            Logger.LogWarning("SgComponentErrorBoundary '{ComponentName}' reached max error count ({MaxErrorCount})",
                ComponentName ?? "Unknown", MaxErrorCount);
        }
    }

    /// <summary>Called when a child component throws.</summary>
    public void HandleError(Exception exception)
    {
        if (_isDisposed) return;

        _errorCount++;
        _currentError = exception;

        Logger.LogError(exception,
            "Error in component '{ComponentName}' (error #{ErrorCount})",
            ComponentName ?? "Unknown", _errorCount);

        // Исправление: RecordError теперь доступен
        Diagnostics?.RecordError(ComponentName ?? GetType().Name, exception);

        StateHasChanged();
    }

    /// <summary>Try to recover from error.</summary>
    public async Task RecoverAsync()
    {
        if (_isDisposed || _isRecovering) return;

        _isRecovering = true;
        try
        {
            _currentError = null;
            StateHasChanged();

            // Brief delay to allow UI to update
            await Task.Delay(100);

            if (!_isDisposed)
                StateHasChanged();
        }
        finally
        {
            _isRecovering = false;
        }
    }

    /// <summary>Reset error count (for page navigation, etc.).</summary>
    public void Reset()
    {
        _currentError = null;
        _errorCount = 0;
        StateHasChanged();
    }

    public void Dispose()
    {
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
