using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Services.Data;

public class SgDexieService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private int _disposed;

    public SgDexieService(IJSRuntime js)
    {
        _js = js;
    }

    private async Task<bool> EnsureModuleAsync(CancellationToken ct = default)
    {
        if (_module != null) return true;
        await _moduleLock.WaitAsync(ct);
        try
        {
            if (_module == null)
            {
                _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-dexie.js");
            }
            return _module != null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        // IndexedDB / JS module may be unavailable (private mode, blocked storage,
        // CDN failure, prerendering). Fail soft — analytics/storage is optional.
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (Exception) { return false; }
        finally { _moduleLock.Release(); }
    }

    private void EnsureModuleOrThrow()
    {
        if (_module == null) throw new InvalidOperationException("Dexie JS module not available (JS disconnected or cancelled).");
    }

    public async Task<bool> TryInitializeAsync(string dbName, Dictionary<string, string> schema, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return false;
        try
        {
            await _module!.InvokeVoidAsync("initDb", dbName, schema);
            return true;
        }
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public async Task InitializeAsync(string dbName, Dictionary<string, string> schema, CancellationToken ct = default)
    {
        // Never let optional IndexedDB storage crash the renderer.
        await TryInitializeAsync(dbName, schema, ct);
    }

    public async Task AddAsync<T>(string dbName, string tableName, T item, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("add", dbName, tableName, item); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task BulkAddAsync<T>(string dbName, string tableName, IEnumerable<T> items, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("bulkAdd", dbName, tableName, items); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task PutAsync<T>(string dbName, string tableName, T item, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("put", dbName, tableName, item); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task<T?> GetAsync<T>(string dbName, string tableName, object id, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return default;
        try { return await _module!.InvokeAsync<T?>("get", dbName, tableName, id); }
        catch (JSException) { return default; } catch (JSDisconnectedException) { return default; } catch (TaskCanceledException) { return default; } catch (ObjectDisposedException) { return default; } catch (InvalidOperationException) { return default; }
    }

    public async Task<List<T>> GetAllAsync<T>(string dbName, string tableName, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return new();
        try { return await _module!.InvokeAsync<List<T>>("getAll", dbName, tableName) ?? new(); }
        catch (JSException) { return new(); } catch (JSDisconnectedException) { return new(); } catch (TaskCanceledException) { return new(); } catch (ObjectDisposedException) { return new(); } catch (InvalidOperationException) { return new(); }
    }

    public async Task<List<T>> QueryAsync<T>(string dbName, string tableName, object? filter = null, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return new();
        try { return await _module!.InvokeAsync<List<T>>("query", dbName, tableName, filter) ?? new(); }
        catch (JSException) { return new(); } catch (JSDisconnectedException) { return new(); } catch (TaskCanceledException) { return new(); } catch (ObjectDisposedException) { return new(); } catch (InvalidOperationException) { return new(); }
    }

    public async Task DeleteAsync(string dbName, string tableName, object id, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("remove", dbName, tableName, id); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task ClearTableAsync(string dbName, string tableName, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("clearTable", dbName, tableName); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task DeleteDatabaseAsync(string dbName, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return;
        try { await _module!.InvokeVoidAsync("deleteDb", dbName); }
        catch (JSException) { } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public async Task<List<DexieTableInfo>> GetTablesAsync(string dbName, CancellationToken ct = default)
    {
        if (!await EnsureModuleAsync(ct)) return new();
        try { return await _module!.InvokeAsync<List<DexieTableInfo>>("getTables", dbName) ?? new(); }
        catch (JSException) { return new(); } catch (JSDisconnectedException) { return new(); } catch (TaskCanceledException) { return new(); } catch (ObjectDisposedException) { return new(); } catch (InvalidOperationException) { return new(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        var module = Interlocked.Exchange(ref _module, null);
        if (module != null)
        {
            try { await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception) { }
        }
        _moduleLock.Dispose();
    }
}

public class DexieTableInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
