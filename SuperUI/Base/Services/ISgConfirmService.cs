// SuperUI/Base/Services/ISgConfirmService.cs

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>Сервис диалогов подтверждения.</summary>
public interface ISgConfirmService
{
    /// <summary>Показать диалог подтверждения и ждать ответа.</summary>
    Task<bool> ConfirmAsync(SgConfirmOptions options, CancellationToken ct = default);

    /// <summary>Удобный метод с минимальными параметрами.</summary>
    Task<bool> ConfirmAsync(string message,
        string? title = null,
        SgConfirmVariant variant = SgConfirmVariant.Default,
        CancellationToken ct = default);

    /// <summary>Событие для SgConfirmHost.</summary>
    event Action<SgConfirmOptions>? OnConfirmRequested;
}

/// <summary>Параметры диалога подтверждения.</summary>
public sealed class SgConfirmOptions
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public SgConfirmVariant Variant { get; set; } = SgConfirmVariant.Default;
    public string OkText { get; set; } = "OK";
    public string CancelText { get; set; } = "Отмена";
    public bool ShowCancel { get; set; } = true;
    public RenderFragment? Content { get; set; }

    // Internal: TaskCompletionSource для await
    internal TaskCompletionSource<bool> Tcs { get; } = new();
}

/// <summary>Вариант диалога подтверждения.</summary>
public enum SgConfirmVariant
{
    Default,
    Warning,
    Danger,
    Info
}
