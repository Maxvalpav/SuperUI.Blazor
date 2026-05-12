using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления toast-уведомлениями.
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgToastService : IDisposable
{
    private readonly List<SgToastMessage> _toasts = [];
    private readonly Lock _lock = new();
    private int _nextId;
    private bool _disposed;

    /// <summary>Текущие toast-сообщения (snapshot).</summary>
    public IReadOnlyList<SgToastMessage> Toasts
    {
        get { lock (_lock) return [.. _toasts]; }
    }

    /// <summary>Событие изменения списка toast.</summary>
    public event Action? OnChange;

    /// <summary>Показать успешный toast.</summary>
    public SgToastMessage Success(string message, int? durationMs = null)
        => Show(message, SgToastType.Success, durationMs);

    /// <summary>Показать информационный toast.</summary>
    public SgToastMessage Info(string message, int? durationMs = null)
        => Show(message, SgToastType.Info, durationMs);

    /// <summary>Показать предупреждение.</summary>
    public SgToastMessage Warning(string message, int? durationMs = null)
        => Show(message, SgToastType.Warning, durationMs);

    /// <summary>Показать ошибку.</summary>
    public SgToastMessage Error(string message, int? durationMs = null)
        => Show(message, SgToastType.Error, durationMs);

    /// <summary>Показать toast загрузки (без автоскрытия).</summary>
    public SgToastMessage Loading(string message)
        => Show(message, SgToastType.Loading, durationMs: null);

    /// <summary>Показать toast с произвольными параметрами.</summary>
    public SgToastMessage Show(string message, SgToastType type = SgToastType.Default, int? durationMs = 4000)
    {
        if (_disposed) return new SgToastMessage(-1, message, type);

        var toast = new SgToastMessage(
            Id: Interlocked.Increment(ref _nextId),
            Message: message,
            Type: type,
            DurationMs: durationMs,
            CreatedAt: DateTimeOffset.UtcNow);

        lock (_lock) _toasts.Add(toast);

        OnChange?.Invoke();

        if (durationMs.HasValue && durationMs.Value > 0)
        {
            _ = AutoDismissAsync(toast.Id, durationMs.Value);
        }

        return toast;
    }

    /// <summary>Закрыть toast по ID.</summary>
    public void Dismiss(int id)
    {
        bool removed;
        lock (_lock) removed = _toasts.RemoveAll(t => t.Id == id) > 0;
        if (removed) OnChange?.Invoke();
    }

    /// <summary>Закрыть все toast.</summary>
    public void DismissAll()
    {
        lock (_lock) _toasts.Clear();
        OnChange?.Invoke();
    }

    /// <summary>Обновить текст существующего toast (для loading→success паттерна).</summary>
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
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_lock) _toasts.Clear();
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
