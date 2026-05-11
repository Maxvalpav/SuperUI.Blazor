// SuperUI/Base/SgVirtualizedDataBase.cs
// ИСПРАВЛЕНО:
// 1. Items.Count() заменён на TryGetNonEnumeratedCount — O(1) для IList
// 2. VirtualItemsProvider — правильная обработка CancellationToken из запроса

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для виртуализированных компонентов.
/// Использует Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize{TItem}.
/// </summary>
public abstract class SgVirtualizedDataBase<TItem> : SgDataBase<TItem>
{
    [Parameter] public int ItemHeight { get; set; } = 40;
    [Parameter] public int OverscanCount { get; set; } = 3;
    [Parameter] public bool UseVirtualization { get; set; } = true;

    // ИСПРАВЛЕНО: Items кэшируем как IReadOnlyList если возможно
    private IReadOnlyList<TItem>? _cachedItemsList;
    private IEnumerable<TItem>? _lastItemsRef;

    /// <summary>
    /// Провайдер для Virtualize — вызывается при каждом скролле.
    /// ИСПРАВЛЕНО: count вычисляется O(1) через TryGetNonEnumeratedCount.
    /// </summary>
    protected async ValueTask<ItemsProviderResult<TItem>> VirtualItemsProvider(
        ItemsProviderRequest request)
    {
        if (DataSource != null)
        {
            var dataRequest = new SgDataRequest
            {
                Page = (request.StartIndex / Math.Max(1, PageSize)) + 1,
                PageSize = request.Count,
                Sort = CurrentSort,
                Filters = CurrentFilters
            };
            try
            {
                // ИСПРАВЛЕНО: передаём request.CancellationToken (от Virtualize)
                // объединённый с ComponentToken
                using var cts = CancellationTokenSource
                    .CreateLinkedTokenSource(request.CancellationToken, ComponentToken);
                var result = await DataSource(dataRequest, cts.Token);
                return new ItemsProviderResult<TItem>(result.Items, result.TotalCount);
            }
            catch (OperationCanceledException)
            {
                return new ItemsProviderResult<TItem>([], 0);
            }
        }

        var itemsSource = Items ?? [];

        // ИСПРАВЛЕНО: кэшируем Items как IReadOnlyList для O(1) Count
        if (!ReferenceEquals(itemsSource, _lastItemsRef))
        {
            _lastItemsRef = itemsSource;
            _cachedItemsList = itemsSource is IReadOnlyList<TItem> rl
                ? rl
                : [.. itemsSource];
        }

        var list = _cachedItemsList!;
        var totalCount = list.Count;
        var slice = list.Skip(request.StartIndex).Take(request.Count);
        return new ItemsProviderResult<TItem>(slice, totalCount);
    }
}
