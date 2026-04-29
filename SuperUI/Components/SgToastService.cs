using Microsoft.Extensions.Options;
using SuperUI;

namespace SuperUI.Components;

/// <summary>
/// Represents a single toast notification.
/// </summary>
public sealed class SgToast : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this toast.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the toast title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the toast message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the toast variant: "default", "success", "danger", or "warn".
    /// </summary>
    public string Variant { get; set; } = "default";

    /// <summary>
    /// Gets or sets the duration in milliseconds before the toast auto-dismisses.
    /// </summary>
    public int DurationMs { get; set; } = 4000;

    /// <summary>
    /// Gets or sets the CancellationTokenSource for this toast's timeout.
    /// </summary>
    public CancellationTokenSource? TimeoutCts { get; set; }

    /// <summary>
    /// Disposes the timeout CancellationTokenSource.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (TimeoutCts != null)
        {
            try
            {
                TimeoutCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
            TimeoutCts.Dispose();
            TimeoutCts = null;
        }
        await ValueTask.CompletedTask;
    }
}

/// <summary>
/// Service for displaying toast notifications. Register via <see cref="SuperUI.ServiceCollectionExtensions.AddSuperUI(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
public sealed class SgToastService : IAsyncDisposable
{
    private readonly int _defaultDurationMs;
    private readonly Dictionary<string, SgToast> _activeToasts = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SgToastService"/>.
    /// </summary>
    public SgToastService() : this(null) { }

    /// <summary>
    /// Initializes a new instance of <see cref="SgToastService"/> with options.
    /// </summary>
    public SgToastService(IOptions<SuperUiOptions>? options)
    {
        _defaultDurationMs = options?.Value.DefaultToastDurationMs ?? 4000;
    }

    /// <summary>
    /// Event raised when a new toast is added.
    /// </summary>
    public event Action<SgToast>? Added;

    /// <summary>
    /// Event raised when a toast is removed.
    /// </summary>
    public event Action<string>? Removed;

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="variant">Toast variant: "default", "success", "danger", or "warn".</param>
    /// <param name="durationMs">Duration in milliseconds. Uses default if not specified.</param>
    public void Show(string message, string? title = null, string variant = "default", int? durationMs = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SgToastService));

        var t = new SgToast
        {
            Message = message,
            Title = title,
            Variant = variant,
            DurationMs = durationMs ?? _defaultDurationMs
        };
        
        _activeToasts[t.Id] = t;
        Added?.Invoke(t);
    }

    /// <summary>
    /// Shows a success toast notification.
    /// </summary>
    public void Success(string message, string? title = null) => Show(message, title, "success");

    /// <summary>
    /// Shows an error toast notification.
    /// </summary>
    public void Error(string message, string? title = null) => Show(message, title, "danger");

    /// <summary>
    /// Shows a warning toast notification.
    /// </summary>
    public void Warn(string message, string? title = null) => Show(message, title, "warn");

    /// <summary>
    /// Shows an info toast notification.
    /// </summary>
    public void Info(string message, string? title = null) => Show(message, title, "default");

    /// <summary>
    /// Dismisses a toast by its ID.
    /// </summary>
    public void Dismiss(string id)
    {
        if (_disposed) return;
        
        if (_activeToasts.Remove(id, out var toast))
        {
            // Cancel the timeout token when manually dismissed
            if (toast.TimeoutCts != null)
            {
                try { toast.TimeoutCts.Cancel(); } catch { }
            }
        }
        
        Removed?.Invoke(id);
    }

    /// <summary>
    /// Disposes the service and cancels all active toast timeouts.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Cancel all active toast timeouts
        foreach (var toast in _activeToasts.Values)
        {
            await toast.DisposeAsync();
        }
        _activeToasts.Clear();
    }
}
