// SuperUI/Base/Services/SgConfirmService.cs

// ИСПРАВЛЕНИЯ:
// ✅ CS0311: реализует ISgConfirmService (все члены)

using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис подтверждающих диалогов.
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgConfirmService : ISgConfirmService
{
    private readonly List<SgConfirmRequest> _pendingRequests = [];
    private readonly Lock _lock = new();

    // ── ISgConfirmService ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<SgConfirmRequest> PendingRequests
    {
        get { lock (_lock) return [.. _pendingRequests]; }
    }

    /// <inheritdoc/>
    public event Action? OnChange;

    /// <inheritdoc/>
    public event Func<SgConfirmRequest, Task<bool>>? Requested;

    /// <inheritdoc/>
    public Task<bool> ConfirmAsync(string message,
        string? title = null,
        string confirmText = "OK",
        string cancelText = "Cancel",
        SgAlertVariant variant = SgAlertVariant.Info)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new SgConfirmRequest(
            Id: Guid.NewGuid(),
            Message: message,
            Title: title,
            ConfirmText: confirmText,
            CancelText: cancelText,
            Variant: variant,
            Result: tcs);

        lock (_lock) _pendingRequests.Add(request);
        OnChange?.Invoke();
        
        // Вызвать обработчик Requested если подписан
        if (Requested is not null)
        {
            _ = Requested.Invoke(request);
        }

        return tcs.Task;
    }

    /// <inheritdoc/>
    public void Respond(Guid id, bool confirmed)
    {
        SgConfirmRequest? request = null;

        lock (_lock)
        {
            var idx = _pendingRequests.FindIndex(r => r.Id == id);
            if (idx >= 0)
            {
                request = _pendingRequests[idx];
                _pendingRequests.RemoveAt(idx);
            }
        }

        request?.Result.TrySetResult(confirmed);
        if (request is not null) OnChange?.Invoke();
    }
}
