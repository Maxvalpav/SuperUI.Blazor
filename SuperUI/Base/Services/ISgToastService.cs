// SuperUI/Base/Services/ISgToastService.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS0535: сигнатуры методов совпадают с SgToastService
//           Show(SgToastOptions) → Show(string, SgToastType, int?)
//           Close(string) → Dismiss(int)
//           CloseAll() → DismissAll()
//           Success/Error/Warning/Info — сигнатуры как в реализации
//           ActiveToasts → Toasts
// ПОДХОД: приведение интерфейса к реализации (не наоборот)

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>Сервис toast-уведомлений.</summary>
public interface ISgToastService : IAsyncDisposable
{
    // ── Текущие toast ────────────────────────────────────────────────────────
    /// <summary>Текущие toast-сообщения (snapshot).</summary>
    IReadOnlyList<SgToastMessage> Toasts { get; }

    // ── События ─────────────────────────────────────────────────────────────
    /// <summary>Событие добавления toast.</summary>
    event Action<SgToastMessage>? Added;

    /// <summary>Событие удаления toast.</summary>
    event Action<SgToastMessage>? Removed;

    /// <summary>Общее событие изменения (обратная совместимость).</summary>
    event Action? OnChange;

    // ── Показ ────────────────────────────────────────────────────────────────
    /// <summary>Показать toast с произвольными параметрами.</summary>
    SgToastMessage Show(string message,
        SgToastType type = SgToastType.Default,
        int? durationMs = 4000);

    /// <summary>Показать успешный toast.</summary>
    SgToastMessage Success(string message, int? durationMs = null);

    /// <summary>Показать информационный toast.</summary>
    SgToastMessage Info(string message, int? durationMs = null);

    /// <summary>Показать предупреждение.</summary>
    SgToastMessage Warning(string message, int? durationMs = null);

    /// <summary>Показать ошибку.</summary>
    SgToastMessage Error(string message, int? durationMs = null);

    /// <summary>Показать toast загрузки (без автоскрытия).</summary>
    SgToastMessage Loading(string message);

    // ── Управление ───────────────────────────────────────────────────────────
    /// <summary>Закрыть toast по ID.</summary>
    void Dismiss(int id);

    /// <summary>Закрыть все toast.</summary>
    void DismissAll();

    /// <summary>Обновить toast (loading → success паттерн).</summary>
    void Update(int id, string message,
        SgToastType type = SgToastType.Success, int? durationMs = 3000);
}

/// <summary>Toast-сообщение.</summary>
public sealed record SgToastMessage(
    int Id,
    string Message,
    SgToastType Type = SgToastType.Default,
    int? DurationMs = 4000,
    DateTimeOffset CreatedAt = default,
    string? Title = null,
    string? Icon = null,
    bool IsClosable = true,
    SgPlacement Placement = SgPlacement.TopRight);

/// <summary>Параметры toast (для API совместимости).</summary>
public sealed class SgToastOptions
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public SgToastType Type { get; set; } = SgToastType.Default;
    public SgPlacement Placement { get; set; } = SgPlacement.TopRight;
    public int DurationMs { get; set; } = 4000;
    public bool ShowClose { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public RenderFragment? Content { get; set; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}

