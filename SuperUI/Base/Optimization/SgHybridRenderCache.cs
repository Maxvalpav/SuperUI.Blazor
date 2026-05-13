// SuperUI/Base/Optimization/SgHybridRenderCache.cs
// 🆕 Гибридный кэш рендеринга для Static SSR + Interactive компонентов.
// Кэширует RenderTree для Static SSR компонентов.
// Автоматическая инвалидация при изменении параметров.
// Ни у кого нет — огромный прирост для SSR-страниц.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Configuration for HybridRenderCache.
/// </summary>
public sealed class SgRenderCacheOptions
{
    /// <summary>Maximum number of cached render trees.</summary>
    public int MaxCachedComponents { get; set; } = 500;

    /// <summary>Cache entry lifetime without access.</summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Absolute cache entry lifetime.</summary>
    public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Enable cache for InteractiveServer mode too (default: Static SSR only).</summary>
    public bool CacheInteractiveServer { get; set; } = false;

    /// <summary>Compress cached render tree bytes (for large trees).</summary>
    public bool CompressCacheEntries { get; set; } = false;
}

/// <summary>
/// Cached render tree entry.
/// </summary>
public sealed class CachedRenderTree
{
    public byte[]? RenderTreeBytes { get; set; }
    public IReadOnlyDictionary<string, object?> ParameterSnapshot { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public string ComponentId { get; init; } = null!;
}

/// <summary>
/// Hybrid render cache that stores pre-rendered RenderTrees for Static SSR components.
/// Automatically invalidates when parameters change.
/// 
/// Usage:
/// - Register as singleton in DI: services.AddSingleton&lt;SgHybridRenderCache&gt;();
/// - Components call: _renderCache.TryGetCachedTree(componentId, out cachedTree);
/// - Invalidated on parameter change via NotifyParameterChanged().
/// </summary>
public sealed class SgHybridRenderCache : IDisposable
{
    private readonly ILogger<SgHybridRenderCache> _logger;
    private readonly SgRenderCacheOptions _options;
    private readonly MemoryCache _cache;
    private readonly ConcurrentDictionary<string, string> _componentToLastParams = new();
    private readonly ConcurrentDictionary<string, int> _hits = new();
    private readonly ConcurrentDictionary<string, int> _misses = new();

    private long _totalHits;
    private long _totalMisses;
    private long _totalInvalidations;
    private long _totalEvictions;

    // Stats
    public long TotalHits => Interlocked.Read(ref _totalHits);
    public long TotalMisses => Interlocked.Read(ref _totalMisses);
    public long TotalInvalidations => Interlocked.Read(ref _totalInvalidations);
    public long TotalEvictions => Interlocked.Read(ref _totalEvictions);
    public double HitRatio => TotalHits + TotalMisses > 0
        ? (double)TotalHits / (TotalHits + TotalMisses)
        : 0;

    public SgHybridRenderCache(ILogger<SgHybridRenderCache> logger,
        IOptions<SgRenderCacheOptions>? options = null)
    {
        _logger = logger;
        _options = options?.Value ?? new SgRenderCacheOptions();
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxCachedComponents,
            ExpirationScanFrequency = TimeSpan.FromMinutes(1),
            CompactionPercentage = 0.25
        });
    }

    /// <summary>
    /// Try to get a cached render tree for the specified component + parameter key.
    /// Returns null if not cached or parameters changed.
    /// </summary>
    public CachedRenderTree? TryGet(string componentId, IReadOnlyDictionary<string, object?> currentParams)
    {
        var cacheKey = BuildCacheKey(componentId, currentParams, out var paramHash);

        if (_cache.TryGetValue(cacheKey, out CachedRenderTree? cached))
        {
            cached.LastAccessed = DateTimeOffset.UtcNow;
            cached.AccessCount++;
            Interlocked.Increment(ref _totalHits);
            _hits.AddOrUpdate(componentId, 1, (_, v) => v + 1);

            _logger.LogDebug("[RenderCache] HIT {ComponentId} (hash={Hash}, accesses={AccessCount})",
                componentId, paramHash, cached.AccessCount);

            return cached;
        }

        Interlocked.Increment(ref _totalMisses);
        _misses.AddOrUpdate(componentId, 1, (_, v) => v + 1);

        _logger.LogDebug("[RenderCache] MISS {ComponentId} (hash={Hash})", componentId, paramHash);

        return null;
    }

    /// <summary>
    /// Store a render tree in the cache.
    /// </summary>
    public void Store(string componentId, IReadOnlyDictionary<string, object?> parameters,
        byte[] renderTreeBytes)
    {
        var cacheKey = BuildCacheKey(componentId, parameters, out var paramHash);

        var entry = new CachedRenderTree
        {
            RenderTreeBytes = renderTreeBytes,
            ParameterSnapshot = new Dictionary<string, object?>(parameters),
            CreatedAt = DateTimeOffset.UtcNow,
            LastAccessed = DateTimeOffset.UtcNow,
            AccessCount = 0,
            ComponentId = componentId
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = _options.SlidingExpiration,
            AbsoluteExpirationRelativeToNow = _options.AbsoluteExpiration,
            Priority = CacheItemPriority.Normal
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheEntryEvicted);

        _cache.Set(cacheKey, entry, cacheOptions);
        _componentToLastParams[componentId] = paramHash;

        _logger.LogDebug("[RenderCache] STORE {ComponentId} (hash={Hash}, size={Bytes})",
            componentId, paramHash, renderTreeBytes.Length);
    }

    /// <summary>
    /// Invalidate cache for a specific component.
    /// </summary>
    public void Invalidate(string componentId)
    {
        if (_componentToLastParams.TryRemove(componentId, out var lastHash))
        {
            Interlocked.Increment(ref _totalInvalidations);
            _cache.Remove(BuildCacheKey(componentId, lastHash));
            _logger.LogDebug("[RenderCache] INVALIDATE {ComponentId}", componentId);
        }
    }

    /// <summary>
    /// Invalidate all cached entries.
    /// </summary>
    public void InvalidateAll()
    {
        _cache.Compact(1.0); // Remove everything
        _componentToLastParams.Clear();
        _logger.LogInformation("[RenderCache] ALL INVALIDATED");
    }

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public SgRenderCacheStats GetStats()
    {
        return new SgRenderCacheStats
        {
            TotalHits = TotalHits,
            TotalMisses = TotalMisses,
            HitRatio = HitRatio,
            TotalInvalidations = TotalInvalidations,
            TotalEvictions = TotalEvictions,
            CachedCount = _cache.Count,
            MaxSize = _options.MaxCachedComponents,
            TopHitComponents = _hits.OrderByDescending(kv => kv.Value).Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value),
            TopMissComponents = _misses.OrderByDescending(kv => kv.Value).Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    public void Dispose()
    {
        _cache.Dispose();
        _componentToLastParams.Clear();
        _hits.Clear();
        _misses.Clear();
    }

    private static string BuildCacheKey(string componentId,
        IReadOnlyDictionary<string, object?> parameters, out string paramHash)
    {
        paramHash = ComputeParameterHash(parameters);
        return $"{componentId}:{paramHash}";
    }

    private static string BuildCacheKey(string componentId, string paramHash)
        => $"{componentId}:{paramHash}";

    private static string ComputeParameterHash(IReadOnlyDictionary<string, object?> parameters)
    {
        // Fast hash computation using FNV-1a
        unchecked
        {
            uint hash = 2166136261;
            foreach (var kvp in parameters.OrderBy(k => k.Key))
            {
                hash ^= (uint)kvp.Key.GetHashCode();
                hash *= 16777619;
                hash ^= (uint)(kvp.Value?.GetHashCode() ?? 0);
                hash *= 16777619;
            }
            return hash.ToString("x8");
        }
    }

    private void OnCacheEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        Interlocked.Increment(ref _totalEvictions);

        if (reason == EvictionReason.Expired || reason == EvictionReason.Capacity)
        {
            if (value is CachedRenderTree cached)
            {
                _logger.LogDebug("[RenderCache] EVICTED {ComponentId} reason={Reason}",
                    cached.ComponentId, reason);
            }
        }
    }
}

public sealed class SgRenderCacheStats
{
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public double HitRatio { get; init; }
    public long TotalInvalidations { get; init; }
    public long TotalEvictions { get; init; }
    public int CachedCount { get; init; }
    public int MaxSize { get; init; }
    public Dictionary<string, int> TopHitComponents { get; init; } = new();
    public Dictionary<string, int> TopMissComponents { get; init; } = new();
}
