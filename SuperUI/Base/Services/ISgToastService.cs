// SuperUI/Base/Services/ISgToastService.cs

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>Сервис toast-уведомлений.</summary>
public interface ISgToastService
{
    /// <summary>Показать toast.</summary>
    void Show(SgToastOptions options);

    /// <summary>Закрыть toast по ID.</summary>
    void Close(string toastId);

    /// <summary>Закрыть все toast.</summary>
    void CloseAll();

    // Удобные перегрузки
    void Success(string message, string? title = null, int? durationMs = null);
    void Error(string message, string? title = null, int? durationMs = null);
    void Warning(string message, string? title = null, int? durationMs = null);
    void Info(string message, string? title = null, int? durationMs = null);

    /// <summary>Событие изменения списка toast (для SgToastHost).</summary>
    event Action? OnChange;

    /// <summary>Текущие активные toast.</summary>
    IReadOnlyList<SgToastOptions> ActiveToasts { get; }
}

/// <summary>Параметры toast-уведомления.</summary>
public sealed class SgToastOptions
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public SgToastType Type { get; set; } = SgToastType.Default;
    public SgPlacement Placement { get; set; } = SgPlacement.TopRight;
    public int DurationMs { get; set; } = 4000; // 0 = без автозакрытия
    public bool ShowClose { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public RenderFragment? Content { get; set; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}
