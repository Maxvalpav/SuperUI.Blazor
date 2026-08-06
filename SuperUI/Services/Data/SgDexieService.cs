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

    private async Task EnsureModuleAsync()
    {
        if (_module != null) return;
        await _moduleLock.WaitAsync();
        try
        {
            if (_module == null)
            {
                _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-dexie.js");
            }
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        finally { _moduleLock.Release(); }
    }

    public async Task InitializeAsync(string dbName, Dictionary<string, string> schema)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("initDb", dbName, schema);
    }

    public async Task AddAsync<T>(string dbName, string tableName, T item)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("add", dbName, tableName, item);
    }

    public async Task BulkAddAsync<T>(string dbName, string tableName, IEnumerable<T> items)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("bulkAdd", dbName, tableName, items);
    }

    public async Task PutAsync<T>(string dbName, string tableName, T item)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("put", dbName, tableName, item);
    }

    public async Task<T?> GetAsync<T>(string dbName, string tableName, object id)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<T?>("get", dbName, tableName, id);
    }

    public async Task<List<T>> GetAllAsync<T>(string dbName, string tableName)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<List<T>>("getAll", dbName, tableName);
    }

    public async Task<List<T>> QueryAsync<T>(string dbName, string tableName, object? filter = null)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<List<T>>("query", dbName, tableName, filter);
    }

    public async Task DeleteAsync(string dbName, string tableName, object id)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("remove", dbName, tableName, id);
    }

    public async Task ClearTableAsync(string dbName, string tableName)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("clearTable", dbName, tableName);
    }

    public async Task DeleteDatabaseAsync(string dbName)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("deleteDb", dbName);
    }

    public async Task<List<DexieTableInfo>> GetTablesAsync(string dbName)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<List<DexieTableInfo>>("getTables", dbName);
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
            catch (ObjectDisposedException) { }
        }
        _moduleLock.Dispose();
    }
}

public class DexieTableInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
