using Microsoft.JSInterop;
using SuperUI.Services.Data;

namespace SuperUI.Services.Analytics;

public class SgClickData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Value { get; set; } = 1;
    public long Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
}

public class SgHeatmapService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly SgDexieService _dexie;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgHeatmapService>? _dotNetRef;
    private bool _isTracking;
    private bool _isAdminMode;

    public bool IsTracking => _isTracking;
    public bool IsAdminMode => _isAdminMode;

    private const string DbName = "SgAnalyticsDB";
    private const string TableName = "clicks";

    public SgHeatmapService(IJSRuntime js, SgDexieService dexie)
    {
        _js = js;
        _dexie = dexie;
    }

    private async Task EnsureModuleAsync()
    {
        if (_module == null)
        {
            // Initialize Dexie for analytics
            await _dexie.InitializeAsync(DbName, new Dictionary<string, string>
            {
                { TableName, "++id, path, timestamp" }
            });

            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-heatmap.js");
            _dotNetRef = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("init", _dotNetRef);
        }
    }

    public async Task StartTrackingAsync()
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("startTracking");
        _isTracking = true;
    }

    public async Task StopTrackingAsync()
    {
        if (_module != null)
        {
            await _module.InvokeVoidAsync("stopTracking");
        }
        _isTracking = false;
    }

    public async Task ToggleAdminModeAsync(bool active)
    {
        await EnsureModuleAsync();
        _isAdminMode = active;

        if (active)
        {
            // Fetch clicks for current path from IndexedDB
            var currentPath = await _js.InvokeAsync<string>("eval", "window.location.pathname");
            var clicks = await _dexie.GetAllAsync<SgClickData>(DbName, TableName);
            var filteredClicks = clicks.Where(c => c.Path == currentPath).ToList();
            
            await _module!.InvokeVoidAsync("showHeatmap", filteredClicks);
        }
        else
        {
            await _module!.InvokeVoidAsync("hideHeatmap");
        }
    }

    [JSInvokable]
    public async Task SaveClicks(List<SgClickData> clicks)
    {
        await _dexie.BulkAddAsync(DbName, TableName, clicks);
    }

    public async Task ClearDataAsync()
    {
        await _dexie.ClearTableAsync(DbName, TableName);
        if (_isAdminMode)
        {
            await ToggleAdminModeAsync(true); // Refresh
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        _dotNetRef?.Dispose();
    }
}
