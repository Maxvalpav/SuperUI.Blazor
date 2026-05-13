// SuperUI/Base/SgComponentErrorBoundary.razor.cs
// FIXES:
// ✅ CS0103: CurrentException — свойство (объявлено в .razor, доступно везде)
// ✅ CS0120: GetType() → this.GetType() (FIX)
// ✅ CS0103: Recover → RecoverAsync (FIX — имя метода правильное)
// ✅ CS0263: Базовый класс синхронизирован: SgComponentBase (единый с .razor)

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SuperUI.Base.Diagnostics;

namespace SuperUI.Base;

public partial class SgComponentErrorBoundary : SgComponentBase
{
    [Inject] private ComponentDiagnostics? Diagnostics { get; set; }
    [Inject] private ILogger<SgComponentErrorBoundary> Logger { get; set; } = NullLogger<SgComponentErrorBoundary>.Instance;

    private bool _isDisposed;

    protected string? ErrorDescription =>
        CurrentException?.Message ?? "An unexpected error occurred.";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_errorCount >= MaxErrorCount)
        {
            Logger.LogWarning("SgComponentErrorBoundary '{ComponentName}' reached max error count ({MaxErrorCount})",
                ComponentName ?? "Unknown", MaxErrorCount);
        }
    }

    /// <summary>
    /// Called when a child component throws.
    /// </summary>
    public void HandleError(Exception exception)
    {
        if (_isDisposed) return;

        _errorCount++;
        CurrentException = exception;

        Logger.LogError(exception,
            "Error in component '{ComponentName}' (error #{ErrorCount})",
            ComponentName ?? "Unknown", _errorCount);

        // ✅ FIX CS0120: this.GetType() вместо статического GetType()
        Diagnostics?.RecordError(ComponentName ?? this.GetType().Name, exception);

        StateHasChanged();
    }

    /// <summary>
    /// Try to recover from error.
    /// </summary>
    public async Task RecoverAsync()
    {
        if (_isDisposed || _isRecovering) return;

        _isRecovering = true;
        try
        {
            CurrentException = null;
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

    /// <summary>
    /// Reset error count (for page navigation, etc.).
    /// </summary>
    public void Reset()
    {
        CurrentException = null;
        _errorCount = 0;
        StateHasChanged();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
        }
        base.Dispose(disposing);
    }
}
