using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис подтверждающих диалогов.
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgConfirmService
{
    private readonly List<SgConfirmRequest> _pendingRequests = [];
    private readonly Lock _lock = new();

    /// <summary>Событие появления нового запроса подтверждения (для SgConfirmHost).</summary>
    public event Action? OnChange;

    /// <summary>Текущие запросы (snapshot).</summary>
    public IReadOnlyList<SgConfirmRequest> PendingRequests
    {
        get { lock (_lock) return [.. _pendingRequests]; }
    }

    /// <summary>
    /// Показать диалог подтверждения и ожидать ответа пользователя.
    /// </summary>
    /// <param name="message">Текст вопроса.</param>
    /// <param name="title">Заголовок диалога (опционально).</param>
    /// <param name="confirmText">Текст кнопки подтверждения.</param>
    /// <param name="cancelText">Текст кнопки отмены.</param>
    /// <param name="variant">Визуальный вариант.</param>
    public Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string confirmText = "OK",
        string cancelText = "Cancel",
        SgAlertVariant variant = SgAlertVariant.Default)
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

        return tcs.Task;
    }

    /// <summary>Ответить на запрос подтверждения (вызывается из SgConfirmHost).</summary>
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

/// <summary>Запрос подтверждения.</summary>
public sealed record SgConfirmRequest(
    Guid Id,
    string Message,
    string? Title,
    string ConfirmText,
    string CancelText,
    SgAlertVariant Variant,
    TaskCompletionSource<bool> Result);
