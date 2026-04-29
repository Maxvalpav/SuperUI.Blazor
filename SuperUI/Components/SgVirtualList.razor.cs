using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace SuperUI.Components
{
    public partial class SgVirtualList<TItem> : ComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// Gets or sets the collection of items to display in the virtual list.
        /// </summary>
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        
        /// <summary>
        /// Gets or sets the template for rendering each item.
        /// </summary>
        [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }
        
        /// <summary>
        /// Gets or sets the height of each item in pixels.
        /// Default is 40.
        /// </summary>
        [Parameter] public float ItemHeight { get; set; } = 40;

        /// <summary>
        /// Gets or sets an optional item height selector for variable-sized rows.
        /// When not provided, <see cref="ItemHeight"/> is used for all items.
        /// </summary>
        [Parameter] public Func<TItem, double>? ItemHeightSelector { get; set; }
        
        /// <summary>
        /// Gets or sets the total height of the virtual list container.
        /// Default is "400px".
        /// </summary>
        [Parameter] public string Height { get; set; } = "400px";
        
        /// <summary>
        /// Gets or sets additional CSS classes for the container.
        /// </summary>
        [Parameter] public string? CssClass { get; set; }
        
        /// <summary>
        /// Gets or sets the number of extra items to render above and below the visible area.
        /// Default is 3.
        /// </summary>
        [Parameter] public int Overscan { get; set; } = 3;

        /// <summary>
        /// Gets or sets an optional key selector used to preserve the visible anchor item when the data set changes.
        /// </summary>
        [Parameter] public Func<TItem, object?>? ItemKeySelector { get; set; }

        /// <summary>
        /// Gets or sets whether the scroll position should be restored when <see cref="Items"/> changes.
        /// Default is true.
        /// </summary>
        [Parameter] public bool PreserveScrollPositionOnItemsChange { get; set; } = true;

        /// <summary>
        /// Gets or sets whether intersection observers should be attached for viewport and edge tracking.
        /// Default is true.
        /// </summary>
        [Parameter] public bool UseIntersectionObserver { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when the list enters or leaves the viewport.
        /// </summary>
        [Parameter] public EventCallback<bool> ViewportVisibilityChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the top edge sentinel becomes visible.
        /// </summary>
        [Parameter] public EventCallback ReachedStart { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the bottom edge sentinel becomes visible.
        /// </summary>
        [Parameter] public EventCallback ReachedEnd { get; set; }

        /// <summary>
        /// Gets or sets whether automatic load-more behavior is enabled when the end sentinel becomes visible.
        /// </summary>
        [Parameter] public bool AutoLoadMore { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold in pixels for triggering <see cref="ReachedEnd"/> before the end is actually reached.
        /// Default is 0.
        /// </summary>
        [Parameter] public int EndThreshold { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether more data can currently be requested.
        /// </summary>
        [Parameter] public bool CanLoadMore { get; set; } = true;

        /// <summary>
        /// Gets or sets whether a load-more request is already in progress.
        /// </summary>
        [Parameter] public bool IsLoadingMore { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the list reaches the end and should load additional items.
        /// </summary>
        [Parameter] public EventCallback LoadMoreAsync { get; set; }

        /// <summary>
        /// Gets or sets whether to preserve the scroll position when the <see cref="Items"/> collection changes.
        /// Default is true.
        /// </summary>
        [Parameter] public bool PreserveScrollOnItemsChange { get; set; } = true;

        [Inject] private IJSRuntime JS { get; set; } = default!;

        private ElementReference _container;
        private ElementReference _topSentinel;
        private ElementReference _bottomSentinel;
        private DotNetObjectReference<SgVirtualList<TItem>>? _objRef;
        private IJSObjectReference? _module;

        private readonly List<TItem> _itemsSnapshot = new();
        private readonly List<double> _itemOffsets = new();
        private readonly List<double> _itemHeights = new();

        private double _scrollTop;
        private int _startIndex = 0;
        private int _endIndex = 0;
        private double? _pendingScrollRestore;
        private bool _isInViewport = true;
        private bool _startIntersected;
        private bool _endIntersected;
        private int _lastLoadTriggerCount = -1;
        private int _totalCount => _itemsSnapshot.Count;
        private bool _itemsChanged;

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var oldItems = Items;
            await base.SetParametersAsync(parameters);

            if (PreserveScrollOnItemsChange && oldItems != null && !ReferenceEquals(oldItems, Items))
            {                
                _itemsChanged = true;
            }
        }

        protected override void OnParametersSet()
        {
            var previousItems = _itemsSnapshot.ToList();
            var previousOffsets = _itemOffsets.ToList();
            var previousScrollTop = _scrollTop;

            _itemsSnapshot.Clear();
            if (Items is not null)
            {
                _itemsSnapshot.AddRange(Items);
            }

            RebuildLayout();

            if (PreserveScrollPositionOnItemsChange && previousItems.Count > 0 && _itemsSnapshot.Count > 0)
            {
                RestoreScrollPosition(previousItems, previousOffsets, previousScrollTop);
            }
            else
            {
                _scrollTop = ClampScrollTop(_scrollTop);
            }

            if (previousItems.Count != _itemsSnapshot.Count)
            {
                _lastLoadTriggerCount = -1;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);
                _module = await JS.InvokeAsync<IJSObjectReference>("import", "/_content/SuperUI/superui-virtuallist.js");
                await _module.InvokeVoidAsync("init", _container, _objRef, _topSentinel, _bottomSentinel, UseIntersectionObserver, EndThreshold);
            }
            else if (_module is not null)
            {
                if (_itemsChanged)
                {
                    _itemsChanged = false;
                    try
                    {
                        await _module.InvokeVoidAsync("setScrollTop", _container, _scrollTop);
                    }
                    catch (JSException) { }
                }

                try
                {
                    await _module.InvokeVoidAsync("refreshObservers", _container, _topSentinel, _bottomSentinel, UseIntersectionObserver, EndThreshold);
                }
                catch (JSException) { }
                catch (TaskCanceledException) { }
            }

            if (_module is not null && _pendingScrollRestore.HasValue)
            {
                var targetScrollTop = _pendingScrollRestore.Value;
                _pendingScrollRestore = null;

                try
                {
                    await _module.InvokeVoidAsync("setScrollTop", _container, targetScrollTop);
                }
                catch (JSException) { }
                catch (TaskCanceledException) { }
            }
        }

        [JSInvokable]
        public void OnScroll(double scrollTop)
        {
            _scrollTop = ClampScrollTop(scrollTop);
            if (_isInViewport)
            {
                StateHasChanged();
            }
        }

        [JSInvokable]
        public async Task OnViewportVisibilityChanged(bool isVisible)
        {
            _isInViewport = isVisible;
            if (ViewportVisibilityChanged.HasDelegate)
            {
                await ViewportVisibilityChanged.InvokeAsync(isVisible);
            }

            if (isVisible)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        [JSInvokable]
        public async Task OnEdgeIntersectionChanged(string edge, bool isIntersecting)
        {
            if (string.Equals(edge, "start", StringComparison.OrdinalIgnoreCase))
            {
                var shouldFire = isIntersecting && !_startIntersected;
                _startIntersected = isIntersecting;
                if (shouldFire && ReachedStart.HasDelegate)
                {
                    await ReachedStart.InvokeAsync();
                }
            }
            else if (string.Equals(edge, "end", StringComparison.OrdinalIgnoreCase))
            {
                var shouldFire = isIntersecting && !_endIntersected;
                _endIntersected = isIntersecting;
                if (shouldFire && ReachedEnd.HasDelegate)
                {
                    await ReachedEnd.InvokeAsync();
                }

                if (shouldFire)
                {
                    await TryLoadMoreAsync();
                }
            }
        }

        private async Task TryLoadMoreAsync()
        {
            if (!AutoLoadMore ||
                !CanLoadMore ||
                IsLoadingMore ||
                !LoadMoreAsync.HasDelegate ||
                _lastLoadTriggerCount == _itemsSnapshot.Count)
            {
                return;
            }

            _lastLoadTriggerCount = _itemsSnapshot.Count;
            await LoadMoreAsync.InvokeAsync();
        }

        private IEnumerable<VirtualListItem<TItem>> GetVisibleItems()
        {
            if (_itemsSnapshot.Count == 0) return Enumerable.Empty<VirtualListItem<TItem>>();

            var containerHeight = ParseHeight(Height);
            if (containerHeight <= 0) return Enumerable.Empty<VirtualListItem<TItem>>();

            var count = _itemsSnapshot.Count;
            _startIndex = Math.Max(0, FindIndexForOffset(_scrollTop) - Overscan);
            _endIndex = Math.Min(count - 1, FindIndexForOffset(_scrollTop + containerHeight) + Overscan);

            if (_endIndex < _startIndex)
            {
                return Enumerable.Empty<VirtualListItem<TItem>>();
            }

            var result = new List<VirtualListItem<TItem>>(_endIndex - _startIndex + 1);
            for (var index = _startIndex; index <= _endIndex && index < count; index++)
            {
                result.Add(new VirtualListItem<TItem>(index, _itemsSnapshot[index]));
            }

            return result;
        }

        private string GetItemStyle(int index)
        {
            var top = _itemOffsets[index].ToString("0.###", CultureInfo.InvariantCulture);
            var height = _itemHeights[index].ToString("0.###", CultureInfo.InvariantCulture);
            return $"position: absolute; top: {top}px; left: 0; right: 0; height: {height}px;";
        }

        private object GetItemRenderKey(TItem item, int index)
        {
            return ItemKeySelector?.Invoke(item) ?? index;
        }

        private double GetTotalHeight()
        {
            return _itemOffsets.Count == 0 ? 0 : _itemOffsets[^1];
        }

        private void RebuildLayout()
        {
            _itemHeights.Clear();
            _itemOffsets.Clear();
            _itemOffsets.Add(0);

            foreach (var item in _itemsSnapshot)
            {
                var height = GetInitialItemHeight(item);
                _itemHeights.Add(height);
                _itemOffsets.Add(_itemOffsets[^1] + height);
            }

            _scrollTop = ClampScrollTop(_scrollTop);
        }

        private double GetInitialItemHeight(TItem item)
        {
            var height = ItemHeightSelector?.Invoke(item) ?? ItemHeight;
            return Math.Max(1, height);
        }

        [JSInvokable]
        public void OnItemsResized(List<ItemSizeUpdate> updates)
        {
            bool changed = false;
            foreach (var update in updates)
            {
                if (update.Index >= 0 && update.Index < _itemHeights.Count)
                {
                    if (Math.Abs(_itemHeights[update.Index] - update.height) > 0.5)
                    {
                        _itemHeights[update.Index] = update.height;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                RecalculateOffsets();
                StateHasChanged();
            }
        }

        private void RecalculateOffsets()
        {
            _itemOffsets.Clear();
            _itemOffsets.Add(0);
            for (int i = 0; i < _itemHeights.Count; i++)
            {
                _itemOffsets.Add(_itemOffsets[^1] + _itemHeights[i]);
            }
        }

        public class ItemSizeUpdate
        {
            public int Index { get; set; }
            public double height { get; set; }
        }

        private int FindIndexForOffset(double offset)
        {
            if (_itemsSnapshot.Count == 0)
            {
                return 0;
            }

            var target = Math.Clamp(offset, 0, GetTotalHeight());
            var low = 0;
            var high = _itemsSnapshot.Count - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                var start = _itemOffsets[mid];
                var end = _itemOffsets[mid + 1];

                if (target < start)
                {
                    high = mid - 1;
                }
                else if (target >= end)
                {
                    low = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return Math.Clamp(low, 0, _itemsSnapshot.Count - 1);
        }

        private void RestoreScrollPosition(
            IReadOnlyList<TItem> previousItems,
            IReadOnlyList<double> previousOffsets,
            double previousScrollTop)
        {
            if (previousOffsets.Count <= 1)
            {
                _scrollTop = ClampScrollTop(previousScrollTop);
                return;
            }

            var previousIndex = FindIndexForOffset(previousScrollTop, previousItems.Count, previousOffsets);
            if (previousIndex < 0 || previousIndex >= previousItems.Count)
            {
                _scrollTop = ClampScrollTop(previousScrollTop);
                return;
            }

            var anchorItem = previousItems[previousIndex];
            var anchorOffset = previousScrollTop - previousOffsets[previousIndex];

            if (!TryFindItemIndex(anchorItem, out var newIndex))
            {
                _scrollTop = ClampScrollTop(previousScrollTop);
                return;
            }

            var restoredScrollTop = _itemOffsets[newIndex] + Math.Max(0, anchorOffset);
            _scrollTop = ClampScrollTop(restoredScrollTop);
            _pendingScrollRestore = _scrollTop;
        }

        private bool TryFindItemIndex(TItem anchorItem, out int index)
        {
            if (ItemKeySelector is null)
            {
                index = _itemsSnapshot.FindIndex(item => EqualityComparer<TItem>.Default.Equals(item, anchorItem));
                return index >= 0;
            }

            var anchorKey = ItemKeySelector(anchorItem);
            index = _itemsSnapshot.FindIndex(item => Equals(ItemKeySelector(item), anchorKey));
            return index >= 0;
        }

        private static int FindIndexForOffset(double offset, int itemCount, IReadOnlyList<double> offsets)
        {
            if (itemCount == 0 || offsets.Count <= 1)
            {
                return 0;
            }

            var target = Math.Clamp(offset, 0, offsets[^1]);
            var low = 0;
            var high = itemCount - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                var start = offsets[mid];
                var end = offsets[mid + 1];

                if (target < start)
                {
                    high = mid - 1;
                }
                else if (target >= end)
                {
                    low = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return Math.Clamp(low, 0, itemCount - 1);
        }

        private double ClampScrollTop(double scrollTop)
        {
            var maxScrollTop = Math.Max(0, GetTotalHeight() - ParseHeight(Height));
            return Math.Clamp(scrollTop, 0, maxScrollTop);
        }

        private static float ParseHeight(string height)
        {
            if (string.IsNullOrWhiteSpace(height))
            {
                return 400;
            }

            var value = height.EndsWith("px", StringComparison.OrdinalIgnoreCase)
                ? height[..^2]
                : height;

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 400;
        }

        public async ValueTask DisposeAsync()
        {
            if (_module != null)
            {
                try
                {
                    await _module.InvokeVoidAsync("dispose", _container);
                    await _module.DisposeAsync();
                }
                catch (JSException) { }
                catch (TaskCanceledException) { }
                catch (ObjectDisposedException) { }
            }
            _objRef?.Dispose();
        }

        private sealed record VirtualListItem<T>(int Index, T Item);
    }
}
