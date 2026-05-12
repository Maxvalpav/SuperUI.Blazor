using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SuperUI.Base.Services;

/// <summary>
/// In-memory реализация ISgPresenceService.
/// Работает на WASM (single user) и Blazor Server (per-circuit).
/// Для multi-user real-time: переопределите с SignalR Hub.
/// </summary>
public sealed class SgPresenceServiceImpl : ISgPresenceService, IDisposable
{
    private readonly ConcurrentDictionary<string, List<SgPresenceUser>> _presence = new();
    private readonly SgSubject<SgPresenceChangedEvent> _subject = new();
    private bool _disposed;

    // entityKey = $"{entityType}:{entityId}"
    private static string Key(string type, string id) => $"{type}:{id}";

    /// <inheritdoc/>
    public Task<IReadOnlyList<SgPresenceUser>> GetPresenceAsync(string entityType, string entityId)
    {
        var key = Key(entityType, entityId);
        var users = _presence.TryGetValue(key, out var list)
            ? (IReadOnlyList<SgPresenceUser>)list.AsReadOnly()
            : [];
        return Task.FromResult(users);
    }

    /// <inheritdoc/>
    public Task ClaimEditAsync(string entityType, string entityId)
    {
        // В реальной реализации: получить userId из AuthState
        var key = Key(entityType, entityId);
        _presence.GetOrAdd(key, _ => []);
        // TODO: добавить реального пользователя
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReleaseEditAsync(string entityType, string entityId)
    {
        var key = Key(entityType, entityId);
        _presence.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IObservable<SgPresenceChangedEvent> PresenceChanged => _subject;

    /// <inheritdoc/>
    public async IAsyncEnumerable<SgPresenceChangedEvent> StreamPresenceChangesAsync(
        string entityType,
        string entityId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<SgPresenceChangedEvent>();
        var sub = _subject.Subscribe(e =>
        {
            if (e.EntityType == entityType && e.EntityId == entityId)
                channel.Writer.TryWrite(e);
        });

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                yield return evt;
        }
        finally
        {
            sub.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subject.Dispose();
        _presence.Clear();
    }
}
