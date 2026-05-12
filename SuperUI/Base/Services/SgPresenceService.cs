// SuperUI/Base/Services/SgPresenceService.cs

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация сервиса присутствия пользователей.
/// Scoped: per-circuit (Server), per-user (WASM).
/// 
/// Для реального multi-user real-time: переопределите через SignalR Hub.
/// </summary>
public sealed class SgPresenceService : ISgPresenceService
{
    private readonly ConcurrentDictionary<string, SgPresenceUser> _onlineUsers = new();
    private volatile bool _disposed;
    private string? _currentUserId;
    private string? _currentStatus;

    /// <summary>Текущий пользователь находится онлайн.</summary>
    public bool IsOnline => _currentUserId != null;

    /// <summary>Статус текущего пользователя (null = не установлен).</summary>
    public string? Status => _currentStatus;

    /// <summary>Список известных онлайн-пользователей.</summary>
    public IReadOnlyList<SgPresenceUser> OnlineUsers
    {
        get
        {
            if (_disposed) return [];
            return _onlineUsers.Values.ToList();
        }
    }

    /// <summary>Событие изменения присутствия любого пользователя.</summary>
    public event Action<SgPresenceUser>? PresenceChanged;

    /// <summary>Обновить статус текущего пользователя.</summary>
    public Task UpdateStatusAsync(string status, CancellationToken ct = default)
    {
        if (_disposed) return Task.CompletedTask;
        ArgumentNullException.ThrowIfNull(status);

        _currentStatus = status;

        if (_currentUserId != null && _onlineUsers.TryGetValue(_currentUserId, out var user))
        {
            var updated = user with { Status = status };
            _onlineUsers[_currentUserId] = updated;
            PresenceChanged?.Invoke(updated);
        }

        return Task.CompletedTask;
    }

    /// <summary>Установить текущего пользователя как онлайн.</summary>
    public Task SetOnlineAsync(string userId, string? displayName = null, CancellationToken ct = default)
    {
        if (_disposed) return Task.CompletedTask;
        ArgumentNullException.ThrowIfNull(userId);

        _currentUserId = userId;
        var user = new SgPresenceUser(
            UserId: userId,
            DisplayName: displayName ?? userId,
            AvatarUrl: null,
            Status: null,
            LastSeen: DateTimeOffset.UtcNow,
            IsOnline: true);

        _onlineUsers[userId] = user;
        PresenceChanged?.Invoke(user);

        return Task.CompletedTask;
    }

    /// <summary>Установить текущего пользователя как оффлайн.</summary>
    public Task SetOfflineAsync(CancellationToken ct = default)
    {
        if (_disposed) return Task.CompletedTask;

        if (_currentUserId != null && _onlineUsers.TryRemove(_currentUserId, out var user))
        {
            var offline = user with { IsOnline = false, LastSeen = DateTimeOffset.UtcNow };
            PresenceChanged?.Invoke(offline);
        }

        _currentUserId = null;
        _currentStatus = null;

        return Task.CompletedTask;
    }

    /// <summary>Dispose implementation.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await SetOfflineAsync();
        _onlineUsers.Clear();
    }
}
