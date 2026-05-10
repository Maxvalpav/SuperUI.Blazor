using Microsoft.Extensions.Options;
using SuperUI;

namespace SuperUI.Components;

/// <summary>
/// Represents a single toast notification.
/// </summary>
public sealed class SgToast : IAsyncDisposable
{
    /// <summary>Gets the unique identifier for this toast.</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Gets or sets the toast title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the toast message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the toast variant. Default is <see cref="SgToastVariant.Default"/>.</summary>
    public SgToastVariant Variant { get; set; } = SgToastVariant.Default;

    /// <summary>Gets or sets the duration in milliseconds before the toast auto-dismisses.</summary>
    public int DurationMs { get; set; } = 4000;

    /// <summary>Gets or sets the CancellationTokenSource for this toast's timeout.</summary>
    public CancellationTokenSource? TimeoutCts { get; set; }

    /// <summary>Disposes the timeout CancellationTokenSource.</summary>
    public async ValueTask DisposeAsync()
    {
        if (TimeoutCts != null)
        {
            try { TimeoutCts.Cancel(); }
            catch (ObjectDisposedException) { }
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
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <summary>Initializes a new instance of <see cref="SgToastService"/>.</summary>
    public SgToastService() : this(null) { }

    /// <summary>Initializes a new instance of <see cref="SgToastService"/> with options.</summary>
    public SgToastService(IOptions<SuperUiOptions>? options)
    {
        _defaultDurationMs = options?.Value.DefaultToastDurationMs ?? 4000;
    }

    /// <summary>Event raised when a new toast is added.</summary>
    public event Action<SgToast>? Added;

    /// <summary>Event raised when a toast is removed.</summary>
    public event Action<string>? Removed;

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="variant">Toast variant. Default is <see cref="SgToastVariant.Default"/>.</param>
    /// <param name="durationMs">Duration in milliseconds. Uses default if not specified.</param>
    public void Show(string message, string? title = null, SgToastVariant variant = SgToastVariant.Default, int? durationMs = null)
    {
        if (_disposed) return;

        var t = new SgToast
        {
            Message = message,
            Title = title,
            Variant = variant,
            DurationMs = durationMs ?? _defaultDurationMs
        };

        _lock.Wait();
        try
        {
            if (_disposed) return;
            _activeToasts[t.Id] = t;
        }
        finally { _lock.Release(); }

        Added?.Invoke(t);
    }

    /// <summary>Shows a success toast notification.</summary>
    public void Success(string message, string? title = null) => Show(message, title, SgToastVariant.Success);

    /// <summary>Shows an error toast notification.</summary>
    public void Error(string message, string? title = null) => Show(message, title, SgToastVariant.Danger);

    /// <summary>Shows a warning toast notification.</summary>
    public void Warn(string message, string? title = null) => Show(message, title, SgToastVariant.Warn);

    /// <summary>Shows an info toast notification.</summary>
    public void Info(string message, string? title = null) => Show(message, title, SgToastVariant.Default);

    /// <summary>Dismisses a toast by its ID.</summary>
    public void Dismiss(string id)
    {
        if (_disposed) return;

        SgToast? toast;
        _lock.Wait();
        try
        {
            if (!_activeToasts.Remove(id, out toast)) return;
        }
        finally { _lock.Release(); }

        _ = toast.DisposeAsync();
        Removed?.Invoke(id);
    }

    /// <summary>Disposes the service and cancels all active toast timeouts.</summary>
    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var toast in _activeToasts.Values)
                await toast.DisposeAsync();

            _activeToasts.Clear();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
