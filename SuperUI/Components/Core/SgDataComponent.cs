using Microsoft.AspNetCore.Components;

namespace SuperUI.Core;

/// <summary>
/// Request passed to <see cref="SgDataProvider{TItem}"/>. Carries paging, sorting, and
/// the component's <see cref="CancellationToken"/> so providers can abort superseded queries.
/// </summary>
/// <param name="Skip">Number of items to skip (page offset).</param>
/// <param name="Take">Page size, or <c>0</c> for "all".</param>
/// <param name="SortBy">Field to sort by, or <c>null</c>.</param>
/// <param name="SortDescending">Sort direction.</param>
/// <param name="CancellationToken">Cancelled when the request is superseded or the component is disposed.</param>
public readonly record struct SgDataRequest(
    int Skip,
    int Take,
    string? SortBy,
    bool SortDescending,
    CancellationToken CancellationToken);

/// <summary>Result returned by an <see cref="SgDataProvider{TItem}"/>.</summary>
/// <param name="Items">Page of items.</param>
/// <param name="TotalCount">Total number of items across all pages, or <c>-1</c> if unknown.</param>
public readonly record struct SgDataResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount);

/// <summary>
/// Delegate that supplies data on demand. Implementations should respect
/// <see cref="SgDataRequest.CancellationToken"/> and throw <see cref="OperationCanceledException"/>
/// when superseded.
/// </summary>
public delegate Task<SgDataResult<TItem>> SgDataProvider<TItem>(SgDataRequest request);

/// <summary>
/// Base for collection-rendering components (grid, table, list, tree).
/// Accepts either a static <see cref="Items"/> source or an async <see cref="DataProvider"/>,
/// manages paging and sorting state, surfaces an <see cref="State"/> for empty/loading/error
/// rendering, and cancels in-flight requests when parameters change or the component is disposed.
/// </summary>
public abstract class SgDataComponent<TItem> : SgComponentBase
{
    private CancellationTokenSource? _loadCts;
    private SgDataRequest? _lastRequest;

    /// <summary>Static collection of items. Mutually exclusive with <see cref="DataProvider"/>.</summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Async data source. Mutually exclusive with <see cref="Items"/>.</summary>
    [Parameter] public SgDataProvider<TItem>? DataProvider { get; set; }

    /// <summary>Page size; <c>0</c> disables paging.</summary>
    [Parameter] public int PageSize { get; set; }

    /// <summary>Zero-based current page.</summary>
    [Parameter] public int Page { get; set; }

    /// <summary>Field name to sort by, or <c>null</c>.</summary>
    [Parameter] public string? SortBy { get; set; }

    /// <summary>Sort direction. Defaults to ascending.</summary>
    [Parameter] public bool SortDescending { get; set; }

    /// <summary>Template rendered while data is loading.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Template rendered when there are no items.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Template rendered when <see cref="DataProvider"/> threw an exception.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>Raised after a successful load.</summary>
    [Parameter] public EventCallback<SgDataResult<TItem>> OnDataLoaded { get; set; }

    /// <summary>Loaded items for the current page.</summary>
    protected IReadOnlyList<TItem> CurrentItems { get; private set; } = Array.Empty<TItem>();

    /// <summary>Total item count across all pages, or <c>-1</c> if unknown.</summary>
    protected int TotalCount { get; private set; } = -1;

    /// <summary>Lifecycle state used to drive loading / empty / error templates.</summary>
    protected SgState State { get; private set; } = SgState.Idle;

    /// <summary>Exception thrown by the last <see cref="DataProvider"/> call, if any.</summary>
    protected Exception? LastError { get; private set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Items is not null && DataProvider is not null)
            throw new InvalidOperationException(
                $"{GetType().Name}: provide either Items or DataProvider, not both.");

        await ReloadAsync();
    }

    /// <summary>
    /// Forces a reload from the current data source. Cancels any in-flight request.
    /// Safe to call from event handlers; will marshal to the renderer's sync context.
    /// </summary>
    public async Task ReloadAsync()
    {
        if (IsDisposed) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentCt);
        _loadCts = cts;

        if (Items is not null)
        {
            CurrentItems = ApplyClientSide(Items);
            TotalCount = Items.Count;
            State = CurrentItems.Count == 0 ? SgState.Empty : SgState.Success;
            LastError = null;
            await SafeStateHasChangedAsync();
            return;
        }

        if (DataProvider is null)
        {
            CurrentItems = Array.Empty<TItem>();
            TotalCount = 0;
            State = SgState.Empty;
            LastError = null;
            await SafeStateHasChangedAsync();
            return;
        }

        State = SgState.Loading;
        LastError = null;
        await SafeStateHasChangedAsync();

        var skip = PageSize > 0 ? Math.Max(0, Page) * PageSize : 0;
        var request = new SgDataRequest(skip, PageSize, SortBy, SortDescending, cts.Token);
        _lastRequest = request;

        try
        {
            var result = await DataProvider(request).ConfigureAwait(false);
            if (cts.IsCancellationRequested || !ReferenceEquals(_loadCts, cts)) return;

            CurrentItems = result.Items ?? Array.Empty<TItem>();
            TotalCount = result.TotalCount;
            State = CurrentItems.Count == 0 ? SgState.Empty : SgState.Success;
            if (OnDataLoaded.HasDelegate) await OnDataLoaded.InvokeAsync(result);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            LastError = ex;
            State = SgState.Error;
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts)) await SafeStateHasChangedAsync();
        }
    }

    /// <summary>
    /// Override to perform client-side sorting / filtering on a static <see cref="Items"/>
    /// source. Default returns the input unchanged.
    /// </summary>
    protected virtual IReadOnlyList<TItem> ApplyClientSide(IReadOnlyList<TItem> source) => source;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
        }
        base.Dispose(disposing);
    }
}
