// SuperUI/Base/Optimization/SgRenderFragmentCache.cs
// ✅ NEW: кэширование RenderFragment для предотвращения лишних рендеров
// ✅ Аналог useMemo в React, computed в Vue
// ✅ AOT-совместим
// ✅ NET8+: работает во всех режимах рендеринга

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Кэширует RenderFragment и пересоздаёт его только при изменении зависимостей.
/// Предотвращает создание новых лямбд при каждом рендере компонента.
/// </summary>
/// <example>
/// private readonly SgRenderFragmentCache _headerCache = new();
///
/// protected override void BuildRenderTree(RenderTreeBuilder builder)
/// {
///     builder.AddContent(0, _headerCache.Get(Title, static title =>
///         b => b.AddContent(0, title)));
/// }
/// </example>
public sealed class SgRenderFragmentCache<TKey>
    where TKey : notnull
{
    private TKey?           _lastKey;
    private RenderFragment? _cachedFragment;
    private bool            _initialized;

    /// <summary>
    /// Получить закэшированный RenderFragment.
    /// Пересоздаётся только если key изменился.
    /// </summary>
    public RenderFragment Get(TKey key, Func<TKey, RenderFragment> factory)
    {
        if (_initialized && EqualityComparer<TKey>.Default.Equals(_lastKey, key))
            return _cachedFragment!;

        _lastKey        = key;
        _cachedFragment = factory(key);
        _initialized    = true;
        return _cachedFragment;
    }

    /// <summary>Инвалидировать кэш вручную.</summary>
    public void Invalidate()
    {
        _cachedFragment = null;
        _initialized    = false;
    }
}

/// <summary>
/// Версия без параметров — кэширует один RenderFragment.
/// </summary>
public sealed class SgRenderFragmentCache
{
    private RenderFragment? _cachedFragment;
    private int             _version;
    private int             _cachedVersion = -1;

    public RenderFragment Get(Func<RenderFragment> factory)
    {
        if (_cachedFragment is not null && _version == _cachedVersion)
            return _cachedFragment;

        _cachedFragment = factory();
        _cachedVersion  = _version;
        return _cachedFragment;
    }

    /// <summary>Инвалидировать кэш (вызвать при изменении данных).</summary>
    public void Invalidate() => Interlocked.Increment(ref _version);
}
