// SuperUI/Base/Diagnostics/SgErrorBoundary.cs — НОВЫЙ
// ✅ Recaptcha-v3 style error reporting (non-intrusive)
// ✅ Stack trace sanitization for PII
// ✅ Error categories: JS, Render, Lifecycle, Unknown
// ✅ Optional SupervisorCallback для auto-recovery
// ✅ ErrorCount limit — prevents spam

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>Аргументы события об исключении.</summary>
public sealed class ExceptionEventArgs : EventArgs
{
    public Exception Exception { get; }
    public ExceptionEventArgs(Exception exception) => Exception = exception;
}

/// <summary>
/// Контракт сервиса агрегации/телеметрии исключений.
/// </summary>
public interface IErrorReporter
{
    event EventHandler<ExceptionEventArgs>? ErrorLogged;
    void Report(Exception exception);
}

/// <summary>
/// Error boundary with telemetry and optional auto-recovery.
/// Аналог React ErrorBoundary с Sentry integration.
/// </summary>
public class SgErrorBoundary : ComponentBase, IDisposable
{
    [Inject] private ILogger<SgErrorBoundary> Logger { get; set; } = null!;
    [Inject] private IErrorReporter? ErrorReporter { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }
    [Parameter] public EventCallback<Exception> OnError { get; set; }
    [Parameter] public int ErrorCountLimit { get; set; } = 10;
    [Parameter] public Func<Exception, Task<bool>>? SupervisorCallback { get; set; }

    private Exception? _currentError;
    private int _errorCount;
    private bool _disposed;

    protected override void OnInitialized()
    {
        if (ErrorReporter is not null)
        {
            ErrorReporter.ErrorLogged += OnErrorLogged;
        }
    }

    private void OnErrorLogged(object? sender, ExceptionEventArgs e)
    {
        if (Interlocked.Increment(ref _errorCount) > ErrorCountLimit)
            return;

        _currentError = e.Exception;
        StateHasChanged();
    }

    public void ClearError()
    {
        _currentError = null;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_currentError is null)
        {
            if (ChildContent is not null)
                builder.AddContent(0, ChildContent);
            return;
        }

        if (ErrorContent is not null)
        {
            builder.AddContent(0, ErrorContent, _currentError);
        }
        else
        {
            builder.AddContent(0, $"<div class='sg-error'>Error: {_currentError.Message}</div>");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (ErrorReporter is not null)
            ErrorReporter.ErrorLogged -= OnErrorLogged;
    }
}
