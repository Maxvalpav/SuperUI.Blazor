using Microsoft.Extensions.Options;
using SuperUI;
using SuperUI.Localization;

namespace SuperUI.Components;

/// <summary>
/// Represents a confirmation dialog request.
/// </summary>
public sealed class SgConfirmRequest
{
    /// <summary>
    /// Gets or initializes the dialog title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or initializes the confirmation message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the dialog variant: "danger", "warn", "info", or "success".
    /// </summary>
    public string Variant { get; init; } = "danger";

    /// <summary>
    /// Gets or initializes the confirm button text.
    /// </summary>
    public string? ConfirmText { get; init; }

    /// <summary>
    /// Gets or initializes the cancel button text.
    /// </summary>
    public string? CancelText { get; init; }
}

/// <summary>
/// Service for displaying confirmation dialogs. Register via <see cref="SuperUI.ServiceCollectionExtensions.AddSuperUI(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
public sealed class SgConfirmService : IAsyncDisposable
{
    private readonly ISuperUILocalizer _localizer;
    private readonly string _defaultTitle;
    private readonly string _defaultConfirmText;
    private readonly string _defaultCancelText;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SgConfirmService"/>.
    /// </summary>
    public SgConfirmService(ISuperUILocalizer localizer) : this(localizer, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SgConfirmService"/> with options.
    /// </summary>
    public SgConfirmService(ISuperUILocalizer localizer, IOptions<SuperUiOptions>? options)
    {
        _localizer = localizer;
        var o = options?.Value;
        _defaultTitle = o?.DefaultConfirmTitle ?? _localizer["ConfirmationTitle"];
        _defaultConfirmText = o?.DefaultConfirmText ?? _localizer["Confirm"];
        _defaultCancelText = o?.DefaultCancelText ?? _localizer["Cancel"];
    }

    /// <summary>
    /// Event raised when a confirmation is requested.
    /// </summary>
    public event Func<SgConfirmRequest, Task<bool>>? Requested;

    /// <summary>
    /// Shows a confirmation dialog and waits for the user's response.
    /// </summary>
    /// <param name="message">The confirmation message.</param>
    /// <param name="title">Optional dialog title.</param>
    /// <param name="variant">Dialog variant: "danger", "warn", "info", or "success".</param>
    /// <param name="confirmText">Optional confirm button text.</param>
    /// <param name="cancelText">Optional cancel button text.</param>
    /// <returns>True if the user confirmed, false otherwise.</returns>
    public Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string variant = "danger",
        string? confirmText = null,
        string? cancelText = null)
    {
        if (_isDisposed) return Task.FromResult(false);

        var request = new SgConfirmRequest
        {
            Title = string.IsNullOrWhiteSpace(title) ? _defaultTitle : title,
            Message = message,
            Variant = variant,
            ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? _defaultConfirmText : confirmText,
            CancelText = string.IsNullOrWhiteSpace(cancelText) ? _defaultCancelText : cancelText
        };

        var handler = Requested;
        return handler is null ? Task.FromResult(false) : handler.Invoke(request);
    }

    /// <summary>
    /// Asynchronously disposes the service and cleans up event subscriptions.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        // Unsubscribe all event handlers to prevent memory leaks
        Requested = null;
        
        await ValueTask.CompletedTask;
    }
}
