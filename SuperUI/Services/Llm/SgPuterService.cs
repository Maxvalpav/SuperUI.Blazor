using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Services.Llm;

/// <summary>
/// Service for interacting with Puter.js AI, Cloud, Auth and UI features.
/// </summary>
public class SgPuterService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgPuterService>? _selfRef;
    private bool _isDisposed;

    public event Action<string>? OnTokenReceived;
    public event Action<string>? OnChatComplete;
    public event Action<string>? OnError;

    public SgPuterService(IJSRuntime js)
    {
        _js = js;
    }

    private async Task EnsureModuleAsync()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SgPuterService));
        if (_module != null) return;

        await _moduleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(SgPuterService));
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-puter.js");
        }
        finally
        {
            _moduleLock.Release();
        }
    }

    private DotNetObjectReference<SgPuterService> GetSelfRef()
        => _selfRef ??= DotNetObjectReference.Create(this);

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await EnsureModuleAsync();
            return await _module!.InvokeAsync<bool>("isPuterAvailable");
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException)   { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    // AI Features
    public async Task ChatAsync(string message, string? model = null, bool stream = true)
    {
        try
        {
            await EnsureModuleAsync();
            await _module!.InvokeVoidAsync("chat", message, model, stream, GetSelfRef());
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }
    }

    [JSInvokable]
    public void OnTokenReceivedCallback(string token) => OnTokenReceived?.Invoke(token);

    [JSInvokable]
    public void OnChatCompleteCallback(string result) => OnChatComplete?.Invoke(result);

    [JSInvokable]
    public void OnErrorCallback(string error) => OnError?.Invoke(error);

    public async Task<string> Txt2ImgAsync(string prompt)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<string>("txt2img", prompt);
    }

    // Auth
    public async Task<bool> IsSignedInAsync()
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<bool>("isSignedIn");
    }

    public async Task SignInAsync()
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("signIn");
    }

    public async Task SignOutAsync()
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("signOut");
    }

    // Key-Value Store
    public async Task KvSetAsync(string key, string value)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("kvSet", key, value);
    }

    public async Task<string?> KvGetAsync(string key)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<string?>("kvGet", key);
    }

    public async Task KvDelAsync(string key)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("kvDel", key);
    }

    // Cloud Storage (FS)
    public async Task FsWriteAsync(string path, string content)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("fsWrite", path, content);
    }

    public async Task<string> FsReadAsync(string path)
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<string>("fsRead", path);
    }

    // UI Utilities
    public async Task AlertAsync(string message)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("alert", message);
    }

    public async Task NotifyAsync(string message, string title = "Notification")
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("notify", message, title);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var selfRef = _selfRef;
        _selfRef = null;
        selfRef?.Dispose();

        var module = _module;
        _module = null;
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException)   { }
            catch (ObjectDisposedException) { }
        }

        _moduleLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
