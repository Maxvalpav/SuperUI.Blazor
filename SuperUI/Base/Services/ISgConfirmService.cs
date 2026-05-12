// SuperUI/Base/Services/ISgConfirmService.cs

// ИСПРАВЛЕНИЯ:
// ✅ CS0311: сигнатура совпадает с SgConfirmService
// ПОДХОД: интерфейс приведён к реализации

using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>Сервис диалогов подтверждения.</summary>
public interface ISgConfirmService
{
    // ── Основной API ────────────────────────────────────────────────────────

    /// <summary>Показать диалог подтверждения и ждать ответа.</summary>
    Task<bool> ConfirmAsync(string message,
        string? title = null,
        string confirmText = "OK",
        string cancelText = "Cancel",
        SgAlertVariant variant = SgAlertVariant.Info);

    // ── Для SgConfirmHost ────────────────────────────────────────────────────

    /// <summary>Текущие запросы (для SgConfirmHost).</summary>
    IReadOnlyList<SgConfirmRequest> PendingRequests { get; }

    /// <summary>Событие появления нового запроса (для SgConfirmHost).</summary>
    event Action? OnChange;

    /// <summary>Событие запроса подтверждения (для SgConfirmHost).</summary>
    event Func<SgConfirmRequest, Task<bool>>? Requested;

    /// <summary>Ответить на запрос (вызывается из SgConfirmHost).</summary>
    void Respond(Guid id, bool confirmed);
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
