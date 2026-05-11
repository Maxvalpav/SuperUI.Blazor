// Файл: Components/Base/SgDataBase.cs
// Зависимости: SgInteractiveBase (уровень 2)

using Microsoft.AspNetCore.Components;
using SuperUI.State;
using SuperUI.Utilities;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 3C: Базовый класс для компонентов отображения данных
/// (DataGrid, TreeView, List, VirtualList...).
/// 
/// РЕАЛИЗУЕТ:
/// - Items (sync) и ItemsProvider (async) источники данных
/// - Loading/Empty states
/// - Selection (single/multi)
/// - Pagination hook
/// </summary>
/// <typeparam name="TItem">Тип элемента данных.</typeparam>
public abstract class SgDataBase<TItem> : SgInteractiveBase
{
    // ── ParameterState ────────────────────────────────────────────────────────

    protected readonly ParameterState<IEnumerable<TItem>?> _itemsState;

    protected SgDataBase()
    {
        using var scope = CreateRegisterScope();
        _itemsState = scope.RegisterParameter<IEnumerable<TItem>?>(nameof(Items))
            .WithParameter(() => Items)
            .WithChangeHandler(OnItemsChangedAsync);
    }

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Синхронный источник данных.</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Асинхронный провайдер данных (virtualization, server-side paging).</summary>
    [Parameter] public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>Размер страницы (0 = нет пагинации).</summary>
    [Parameter] public int PageSize { get; set; } = 0;

    /// <summary>Текущая страница (0-based).</summary>
    [Parameter] public int CurrentPage { get; set; } = 0;
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }

    /// <summary>Разрешить множественный выбор.</summary>
    [Parameter] public bool MultiSelect { get; set; }

    /// <summary>Выбранный элемент (single select).</summary>
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Выбранные элементы (multi select).</summary>
    [Parameter] public IReadOnlyList<TItem>? SelectedItems { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Template для пустого состояния.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Template для состояния загрузки.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    // ── Состояние ─────────────────────────────────────────────────────────────

    private bool _isLoading;
    private int _totalCount;
    private List<TItem> _currentItems = new();
    private readonly HashSet<TItem> _selectedSet = new();

    protected bool IsLoadingData => _isLoading || Loading;
    protected bool IsEmpty => !_isLoading && !_currentItems.Any();
    protected IReadOnlyList<TItem> CurrentItems => _currentItems;
    protected int TotalCount => _totalCount;

    // ── Data loading ──────────────────────────────────────────────────────────

    protected override async ValueTask OnComponentInitializedAsync(CancellationToken ct)
    {
        await base.OnComponentInitializedAsync(ct);
        await LoadDataAsync(ct);
    }

    /// <summary>Перезагрузить данные (например, после фильтрации).</summary>
    public async Task ReloadAsync()
    {
        var token = _lifecycleToken.Renew();
        await LoadDataAsync(token);
        await RequestStateUpdateAsync();
    }

    private async ValueTask OnItemsChangedAsync()
    {
        await LoadDataAsync(_lifecycleToken.Current);
    }

    private async ValueTask LoadDataAsync(CancellationToken ct)
    {
        _isLoading = true;
        await RequestStateUpdateAsync();

        try
        {
            if (ItemsProvider is not null)
            {
                // Async provider
                var request = new ItemsProviderRequest(
                    CurrentPage * PageSize,
                    PageSize > 0 ? PageSize : int.MaxValue,
                    ct);

                var result = await ItemsProvider(request);
                _currentItems = result.Items?.ToList() ?? new();
                _totalCount = result.TotalItemCount;
            }
            else if (_itemsState.Value is not null)
            {
                // Sync items — применяем пагинацию если нужна
                var all = _itemsState.Value.ToList();
                _totalCount = all.Count;
                _currentItems = PageSize > 0
                    ? all.Skip(CurrentPage * PageSize).Take(PageSize).ToList()
                    : all;
            }
            else
            {
                _currentItems = new();
                _totalCount = 0;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Нормально — загрузка отменена
        }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(LoadDataAsync));
            _currentItems = new();
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                _isLoading = false;
                await RequestStateUpdateAsync();
            }
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    protected bool IsSelected(TItem item)
        => MultiSelect ? _selectedSet.Contains(item) : EqualityComparer<TItem>.Default.Equals(SelectedItem, item);

    protected async Task ToggleSelectionAsync(TItem item)
    {
        if (MultiSelect)
        {
            if (!_selectedSet.Add(item))
                _selectedSet.Remove(item);

            var selected = _selectedSet.ToList().AsReadOnly();
            if (SelectedItemsChanged.HasDelegate)
                await SelectedItemsChanged.InvokeAsync(selected);
        }
        else
        {
            if (SelectedItemChanged.HasDelegate)
                await SelectedItemChanged.InvokeAsync(item);
        }

        await RequestStateUpdateAsync();
    }

    // ── Пагинация ─────────────────────────────────────────────────────────────

    protected int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)_totalCount / PageSize) : 1;

    protected async Task GoToPageAsync(int page)
    {
        if (page < 0 || (TotalPages > 0 && page >= TotalPages)) return;

        if (CurrentPageChanged.HasDelegate)
            await CurrentPageChanged.InvokeAsync(page);

        await LoadDataAsync(_lifecycleToken.Current);
    }

    // ── ARIA ──────────────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> GetAriaAttributes()
    {
        var attrs = (Dictionary<string, object?>)base.GetAriaAttributes();
        attrs["aria-rowcount"] = _totalCount.ToString();
        if (IsLoadingData) attrs["aria-busy"] = "true";
        return attrs;
    }

    protected override string GetComponentPrefix() => "data";
}

/// <summary>Делегат для асинхронной загрузки данных.</summary>
public delegate ValueTask<ItemsProviderResult<TItem>> ItemsProviderDelegate<TItem>(ItemsProviderRequest request);

/// <summary>Запрос данных для асинхронного провайдера.</summary>
public sealed class ItemsProviderRequest
{
    public int StartIndex { get; }
    public int Count { get; }
    public CancellationToken CancellationToken { get; }

    public ItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        CancellationToken = cancellationToken;
    }
}

/// <summary>Результат асинхронной загрузки данных.</summary>
public sealed class ItemsProviderResult<TItem>
{
    public IEnumerable<TItem> Items { get; }
    public int TotalItemCount { get; }

    public ItemsProviderResult(IEnumerable<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}
