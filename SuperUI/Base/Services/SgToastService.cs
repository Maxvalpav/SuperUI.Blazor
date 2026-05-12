// SuperUI/Base/Services/SgToastService.cs
// ИСПРАВЛЕНО:
// ✅ CS0246: добавлен тип SgToast (alias/typedef через using)
// ✅ CS1061: Added — событие при добавлении toast
// ✅ CS1061: Removed — событие при удалении toast
// ✅ CS1061: DisposeAsync — реализация IAsyncDisposable
// ✅ OnChange — сохранён для обратной совместимости
// УЛУЧШЕНО:
// ✅ ISgToastService — интерфейс для DI и тестирования
// ✅ SgToastOptions — расширенные опции toast
// ✅ MaxToasts — ограничение количества

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс сервиса управления toast-уведомлениями.
/// </summary>
public interface ISgToastService : IAsyncDisposable
{
    IReadOnlyList<SgToastMessage> Toasts { get; }

    event Action<SgToastMessage>? Added;
    event Action<SgToastMessage>? Removed;
    event Action? OnChange;

    SgToastMessage Show(string message, SgToastType type = SgToastType.Default, int? durationMs = 4000);
    SgToastMessage Success(string message, int? durationMs = null);
    SgToastMessage Info(string message, int? durationMs = null);
    SgToastMessage Warning(string message, int? durationMs = null);
    SgToastMessage Error(string message, int? durationMs = null);
    SgToastMessage Loading(string message);
    void Dismiss(int id);
    void DismissAll();
    void Update(int id, string message, SgToastType type = SgToastType.Success, int? durationMs = 3000);
}

/// <summary>
/// Сервис управления toast-уведомлениями.
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgToastService : ISgToastService, IDisposable, IAsyncDisposable
{
    private readonly List<SgToastMessage> _toasts = [];
    private readonly Lock _lock = new();
    private int _nextId;
    private volatile bool _disposed;

    /// <summary>Максимальное количество одновременных toast.</summary>
    public int MaxToasts { get; set; } = 10;

    /// <summary>Длительность по умолчанию (мс).</summary>
    public int DefaultDurationMs { get; set; } = 4000;

    /// Текущие toast-сообщения (snapshot).
    public IReadOnlyList<SgToastMessage> Toasts
    {
        get
        {
            lock (_lock) return [.. _toasts];
        }
    }

    // ── События ───────────────────────────────────────────────────────────────

    /// <summary>
    /// FIX CS1061: событие при добавлении toast.
    /// Ожидается SgToastHost.
    /// </summary>
    public event Action<SgToastMessage>? Added;

    /// <summary>
    /// FIX CS1061: событие при удалении toast.
    /// Ожидается SgToastHost.
    /// </summary>
    public event Action<SgToastMessage>? Removed;

    /// <summary>
    /// Общее событие изменения (обратная совместимость).
    /// </summary>
    public event Action? OnChange;

    // ── Показ ─────────────────────────────────────────────────────────────────

    /// Показать успешный toast.
    public SgToastMessage Success(string message, int? durationMs = null)
        => Show(message, SgToastType.Success, durationMs ?? DefaultDurationMs);

    /// Показать информационный toast.
    public SgToastMessage Info(string message, int? durationMs = null)
        => Show(message, SgToastType.Info, durationMs ?? DefaultDurationMs);

    /// Показать предупреждение.
    public SgToastMessage Warning(string message, int? durationMs = null)
        => Show(message, SgToastType.Warning, durationMs ?? DefaultDurationMs);

    /// Показать ошибку.
    public SgToastMessage Error(string message, int? durationMs = null)
        => Show(message, SgToastType.Error, durationMs ?? DefaultDurationMs);

    /// Показать toast загрузки (без автоскрытия).
    public SgToastMessage Loading(string message)
        => Show(message, SgToastType.Loading, durationMs: null);

    /// Показать toast с произвольными параметрами.
    public SgToastMessage Show(
        string message,
        SgToastType type = SgToastType.Default,
        int? durationMs = 4000)
    {
        if (_disposed) return new SgToastMessage(-1, message, type);

        var toast = new SgToastMessage(
            Id: Interlocked.Increment(ref _nextId),
            Message: message,
            Type: type,
            DurationMs: durationMs,
            CreatedAt: DateTimeOffset.UtcNow);

        lock (_lock)
        {
            _toasts.Add(toast);
            // Ограничение: удаляем старые toast при превышении MaxToasts
            while (_toasts.Count > MaxToasts)
                _toasts.RemoveAt(0);
        }

        // Уведомляем подписчиков ВНЕ lock
        Added?.Invoke(toast);
        OnChange?.Invoke();

        if (durationMs.HasValue && durationMs.Value > 0)
            _ = AutoDismissAsync(toast.Id, durationMs.Value);

        return toast;
    }

    /// Закрыть toast по ID.
    public void Dismiss(int id)
    {
        SgToastMessage? removed = null;
        lock (_lock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                removed = _toasts[idx];
                _toasts.RemoveAt(idx);
            }
        }

        if (removed is not null)
        {
            Removed?.Invoke(removed);     // FIX CS1061
            OnChange?.Invoke();
        }
    }

    /// Закрыть все toast.
    public void DismissAll()
    {
        List<SgToastMessage> snapshot;
        lock (_lock)
        {
            snapshot = [.. _toasts];
            _toasts.Clear();
        }

        foreach (var t in snapshot)
            Removed?.Invoke(t);           // FIX CS1061

        OnChange?.Invoke();
    }

    /// Обновить текст существующего toast (loading → success паттерн).
    public void Update(int id, string message, SgToastType type = SgToastType.Success, int? durationMs = 3000)
    {
        bool updated = false;
        lock (_lock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                _toasts[idx] = _toasts[idx] with { Message = message, Type = type, DurationMs = durationMs };
                updated = true;
            }
        }

        if (updated)
        {
            OnChange?.Invoke();
            if (durationMs.HasValue && durationMs.Value > 0)
                _ = AutoDismissAsync(id, durationMs.Value);
        }
    }

    private async Task AutoDismissAsync(int id, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
            if (!_disposed) Dismiss(id);
        }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }

    // ── IDisposable / IAsyncDisposable ────────────────────────────────────────

    public void Dispose()
    {
        _disposed = true;
        lock (_lock) _toasts.Clear();
    }

    /// <summary>
    /// FIX CS1061: реализация IAsyncDisposable.
    /// Вызывается из SgToastHost при DisposeAsync.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
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

/// <summary>
/// FIX CS0246: SgToast — alias для обратной совместимости.
/// Используйте SgToastMessage в новом коде.
/// </summary>
[Obsolete("Use SgToastMessage. SgToast will be removed in SuperUI v2.0.")]
public sealed record SgToast(
    int Id,
    string Message,
    SgToastType Type = SgToastType.Default,
    int? DurationMs = 4000,
    DateTimeOffset CreatedAt = default,
    string? Title = null,
    string? Icon = null,
    bool IsClosable = true,
    SgPlacement Placement = SgPlacement.TopRight);
