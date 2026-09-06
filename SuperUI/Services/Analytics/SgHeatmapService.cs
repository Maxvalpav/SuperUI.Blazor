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
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private int _disposed;

    public bool IsTracking => _isTracking;
    public bool IsAdminMode => _isAdminMode;

    private const string DbName = "SgAnalyticsDB";
    private const string TableName = "clicks";

    public SgHeatmapService(IJSRuntime js, SgDexieService dexie)
    {
        _js = js;
        _dexie = dexie;
    }

    private async Task<bool> EnsureModuleAsync()
    {
        if (_module != null) return true;
        await _moduleLock.WaitAsync();
        try
        {
            if (_module != null) return true;
            // Initialize Dexie for analytics (fail-soft: never crash renderer
            // when IndexedDB is unavailable — private mode, blocked storage, etc.)
            await _dexie.InitializeAsync(DbName, new Dictionary<string, string>
            {
                { TableName, "++id, path, timestamp" }
            });

            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-heatmap.js");
            _dotNetRef = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("init", _dotNetRef);
            return _module != null;
        }
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; } catch (Exception) { return false; }
        finally { _moduleLock.Release(); }
    }

    public async Task StartTrackingAsync()
    {
        try
        {
            if (!await EnsureModuleAsync()) return;
            await _module!.InvokeVoidAsync("startTracking");
            _isTracking = true;
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    public async Task StopTrackingAsync()
    {
        try
        {
            if (_module != null)
            {
                await _module.InvokeVoidAsync("stopTracking");
            }
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        finally { _isTracking = false; }
    }

    public async Task ToggleAdminModeAsync(bool active)
    {
        try
        {
            if (!await EnsureModuleAsync()) return;
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
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    [JSInvokable]
    public async Task SaveClicks(List<SgClickData> clicks)
    {
        try { await _dexie.BulkAddAsync(DbName, TableName, clicks); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task ClearDataAsync()
    {
        try
        {
            await _dexie.ClearTableAsync(DbName, TableName);
            if (_isAdminMode)
            {
                await ToggleAdminModeAsync(true); // Refresh
            }
        }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        var module = Interlocked.Exchange(ref _module, null);
        if (module != null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception) { }
            try { await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception) { }
        }
        _dotNetRef?.Dispose();
        _moduleLock.Dispose();
    }
}
