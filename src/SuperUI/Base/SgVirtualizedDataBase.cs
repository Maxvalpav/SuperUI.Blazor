using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using SuperUI.Base;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для виртуализированных компонентов данных.
/// Использует Virtualize<T> от Microsoft для рендеринга только видимых элементов.
/// Дополнительно: IntersectionObserver API для lazy loading секций.
/// </summary>
public abstract class SgVirtualizedDataBase<TItem> : SgDataBase<TItem>
{
    // ── Параметры ─────────────────────────────────────────────────────────────

    [Parameter] public int ItemHeight { get; set; } = 40; // px
    [Parameter] public int OverscanCount { get; set; } = 3;
    [Parameter] public bool UseVirtualization { get; set; } = true;

    // ── Виртуализация через Blazor Virtualize<T> ──────────────────────────────

    /// <summary>
    /// Провайдер элементов для Virtualize<T>.
    /// Подключает наш DataSource к Blazor-виртуализации.
    /// </summary>
    protected async ValueTask<ItemsProviderResult<TItem>> VirtualItemsProvider(
        ItemsProviderRequest request)
    {
        if (DataSource != null)
        {
            var dataRequest = new SgDataRequest
            {
                Page = (request.StartIndex / PageSize) + 1,
                PageSize = request.Count,
                Sort = CurrentSort,
                Filters = CurrentFilters
            };

            try
            {
                var result = await DataSource(dataRequest, request.CancellationToken);
                return new ItemsProviderResult<TItem>(result.Items, result.TotalCount);
            }
            catch (OperationCanceledException)
            {
                return new ItemsProviderResult<TItem>([], 0);
            }
        }

        var items = (Items ?? [])
            .Skip(request.StartIndex)
            .Take(request.Count);

        return new ItemsProviderResult<TItem>(items, Items?.Count() ?? 0);
    }
}
