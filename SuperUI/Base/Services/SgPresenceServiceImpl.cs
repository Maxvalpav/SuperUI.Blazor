// SuperUI/Base/Services/SgPresenceServiceImpl.cs

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// In-memory реализация ISgPresenceService.
///
/// WASM: один пользователь per-tab. Presence = локальное состояние.
/// Server: per-circuit. Каждый circuit = один пользователь.
/// Multi-user collaboration: переопределите через SignalR Hub.
/// </summary>
public sealed class SgPresenceServiceImpl : ISgPresenceService
{
    private readonly ConcurrentDictionary<string, SgPresenceUser> _onlineUsers = new();
    private string? _currentUserId;
    private string? _currentStatus;
    private volatile bool _disposed;

    // ── ISgPresenceService ───────────────────────────────────────────────────

    /// <inheritdoc />
    public bool IsOnline => _currentUserId is not null && !_disposed;

    /// <inheritdoc />
    public string? Status => _currentStatus;

    /// <inheritdoc />
    public IReadOnlyList<SgPresenceUser> OnlineUsers
        => _onlineUsers.Values.Where(u => u.IsOnline).ToList().AsReadOnly();

    /// <inheritdoc />
    public event Action<SgPresenceUser>? PresenceChanged;

    /// <inheritdoc />
    public Task UpdateStatusAsync(string status, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        _currentStatus = status;

        if (_currentUserId is not null && _onlineUsers.TryGetValue(_currentUserId, out var existing))
        {
            var updated = existing with { Status = status, LastSeen = DateTimeOffset.UtcNow };
            _onlineUsers[_currentUserId] = updated;
            PresenceChanged?.Invoke(updated);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetOnlineAsync(string userId, string? displayName = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _currentUserId = userId;

        var user = new SgPresenceUser(
            UserId: userId,
            DisplayName: displayName,
            AvatarUrl: null,
            Status: _currentStatus,
            LastSeen: DateTimeOffset.UtcNow,
            IsOnline: true);

        _onlineUsers[userId] = user;
        PresenceChanged?.Invoke(user);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetOfflineAsync(CancellationToken ct = default)
    {
        if (_disposed || _currentUserId is null) return Task.CompletedTask;

        if (_onlineUsers.TryGetValue(_currentUserId, out var existing))
        {
            var offline = existing with { IsOnline = false, LastSeen = DateTimeOffset.UtcNow };
            _onlineUsers[_currentUserId] = offline;
            PresenceChanged?.Invoke(offline);
        }

        _currentUserId = null;
        _currentStatus = null;

        return Task.CompletedTask;
    }

    // ── IAsyncDisposable ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Помечаем пользователя офлайн перед очисткой
        await SetOfflineAsync().ConfigureAwait(false);
        _onlineUsers.Clear();
        PresenceChanged = null;
    }

    // ── Вспомогательные методы ───────────────────────────────────────────────

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, nameof(SgPresenceServiceImpl));

    /// <summary>Получить пользователя по userId или null.</summary>
    public SgPresenceUser? GetUser(string userId)
        => _onlineUsers.TryGetValue(userId, out var u) ? u : null;
}
